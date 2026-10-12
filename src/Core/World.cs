using Godot;
using System;
using SpaceGame.UI;

namespace SpaceGame.Core
{
	public partial class World : Node2D
	{
		private ColorRect _spaceGrid;
		private Camera2D _camera;
		private HudController _hud;
		private SpaceGame.Entities.Shuttle.Shuttle _shuttle; // Сохраняем ссылку на шаттл
		public static World Instance { get; private set; }
		private Line2D _routeLine;

		// ЭКСПОРТ ШАБЛОНОВ
		[Export] public PackedScene PlanetTemplate { get; set; }
		[Export] public PackedScene EnemyTemplate { get; set; }
		[Export] public PackedScene EnemyLegionTemplate { get; set; }

		public override void _Ready()
		{
			base._Ready();
			Instance = this; // Позволит шаттлу легко находить мир из любой точки кода
			GD.Print("Космос запущен!");
			
			_spaceGrid = GetNode<ColorRect>("%SpaceGrid");
			_hud = GetNode<HudController>("%HUD");
			_routeLine = GetNode<Line2D>("%AutopilotRouteLine");

			// Находим шаттл
			_shuttle = GetNode<SpaceGame.Entities.Shuttle.Shuttle>("%Shuttle");

			if (_shuttle != null)
			{
				// На всякий случай оставляем подписку
				_shuttle.CameraReady += RegisterCamera;

				if (_hud != null)
				{
					_hud.Initialize(_shuttle);
				}
			}
			// ИНИЦИАЛИЗАЦИЯ И ЗАПУСК ОДНОЙ СТРОКОЙ:
			// Передаем текущий мир (this) и шаблон из инспектора (PlanetTemplate)
			var generator = new SystemGenerator(this, PlanetTemplate);
			generator.GenerateSystemPlanets();

			// ЗАПУСК ЛЕГИОНА
			var legion = EnemyLegionTemplate.Instantiate<SpaceGame.Entities.Enemy.EnemyLegion>();
			legion.GlobalPosition = new Vector2(2000, 2000);
			AddChild(legion);

		}

		public override void _Process(double delta)
		{
			base._Process(delta);

			// 1. Камера должна приходить через сигнал CameraReady → RegisterCamera
			if (_camera == null && _shuttle != null)
			{
				_camera = _shuttle.GetNodeOrNull<Camera2D>("%Camera2D");
			}

			// 2. ИСПРАВЛЕННЫЙ РЕНДЕР ЛИНИИ МАРШРУТА
			var shuttleNode = _shuttle;
			if (_routeLine != null && shuttleNode != null)
			{
				// ХИТРЫЙ ГЕЙМДИЗАЙНЕРСКИЙ ФИКС:
				// Линия маршрута отображается ТОЛЬКО если автопилот активен И корабль находится в режиме полета (APPROACHING).
				// Если идет стадия SELECTING... или корабль уже LANDED — линия автоматически скроется!
				if (shuttleNode.IsAutopilotActive && shuttleNode.AutopilotExeState == "APPROACHING")
				{
					// Фикс на случай, если TargetPlanetObj сбросился, но имя планеты у нас есть
					if (shuttleNode.TargetPlanetObj == null && shuttleNode.TargetPlanetName != "NONE")
					{
						var planets = GetTree().GetNodesInGroup("planets");
						foreach (Node node in planets)
						{
							if (node is SpaceGame.Entities.Planet.Planet p && p.PlanetName.ToUpper() == shuttleNode.TargetPlanetName)
							{
								_routeLine.Visible = true;
								_routeLine.ClearPoints();
								_routeLine.AddPoint(shuttleNode.GlobalPosition);
								_routeLine.AddPoint(p.GlobalPosition);
								break; 
							}
						}
					}
					// Если объект привязан нормально — рисуем стандартно
					else if (shuttleNode.TargetPlanetObj != null)
					{
						_routeLine.Visible = true;
						_routeLine.ClearPoints();
						_routeLine.AddPoint(shuttleNode.GlobalPosition);
						_routeLine.AddPoint(shuttleNode.TargetPlanetObj.GlobalPosition);
					}
				}
				else
				{
					// Если автопилот выключен, думает или уже сел — полностью отключаем визуал луча
					_routeLine.Visible = false;
				}
			}


			// 3. Отправка координат бесконечной сетки на GPU (Освобождено от влияния условий)
			if (_camera != null && _spaceGrid != null)
			{
				var shaderMaterial = _spaceGrid.Material as ShaderMaterial;
				if (shaderMaterial != null)
				{
					shaderMaterial.SetShaderParameter("camera_offset", _camera.GlobalPosition);
					shaderMaterial.SetShaderParameter("camera_zoom", _camera.Zoom);

					float dynamicGridSize = 100f;
					if (_camera.Zoom.X < 0.6f) dynamicGridSize = 400f;
					if (_camera.Zoom.X < 0.35f) dynamicGridSize = 1000f;

					shaderMaterial.SetShaderParameter("grid_size", dynamicGridSize);
				}
			}
		}


		public void RegisterCamera(Camera2D playerCamera)
		{
			_camera = playerCamera;
			GD.Print("Камера игрока успешно зарегистрирована через Сигнал:", _camera);
		}

		public override void _UnhandledInput(InputEvent @event)
		{
			base._UnhandledInput(@event);

			if (@event is InputEventKey eventKey)
			{
				if (eventKey.Keycode == Godot.Key.F && eventKey.IsPressed())
				{
					if (_hud != null)
					{
						_hud.Visible = !_hud.Visible;
						GD.Print($"Переключение интерфейса. Режим видимости: {_hud.Visible}");
					}
				}
			}
		}
	}
}
