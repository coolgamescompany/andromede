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
		
		public override void _Ready()
		{
			base._Ready();
			Instance = this; // Позволит шаттлу легко находить мир из любой точки кода
			GD.Print("Космос запущен!");
			
			_spaceGrid = GetNode<ColorRect>("%SpaceGrid");
			_hud = GetNode<HudController>("%HUD");

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
		}

		public override void _Process(double delta)
		{
			base._Process(delta);

			// ЖЕЛЕЗОБЕТОННЫЙ ХАК: Если камера до сих пор null, пробуем вытащить её напрямую из шаттла
			if (_camera == null && _shuttle != null)
			{
				// Ищем узел Camera2D внутри сцены шаттла. Измени имя "Camera2D", если у тебя узел называется иначе!
				_camera = _shuttle.GetNodeOrNull<Camera2D>("Camera2D") ?? _shuttle.GetNodeOrNull<Camera2D>("%Camera2D");
				if (_camera != null)
				{
					GD.Print("Камера найдена напрямую через GetNode в _Process!");
				}
			}

			// Если камера успешно нашлась и сетка есть — шлем координаты на GPU
			if (_camera != null && _spaceGrid != null)
			{
				var shaderMaterial = _spaceGrid.Material as ShaderMaterial;
				if (shaderMaterial != null)
				{
					// Передаем точные глобальные координаты камеры и ее зум
					shaderMaterial.SetShaderParameter("camera_offset", _camera.GlobalPosition);
					shaderMaterial.SetShaderParameter("camera_zoom", _camera.Zoom);

					// Сглаживание муара: укрупняем шаг сетки при сильном отдалении
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
