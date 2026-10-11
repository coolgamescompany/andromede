using Godot;
using System;
using System.Collections.Generic;
using Vector2 = Godot.Vector2;

namespace SpaceGame.Entities.Shuttle
{
	public partial class Shuttle : RigidBody2D
	{
		private Camera2D _shuttleCamera;
		private float _targetZoom = 1.0f;
		private float _zoomSpeed = 0.1f;
		private float _minZoom = 0.3f;
		private float _maxZoom = 2.0f;

		private float _maxVelocity = 5000f;
		private float _enginePower = 1500f;

		[Export] public float CurrentThrust { get; set; } = 0.0f; 
		private float _thrustAcceleration = 3.0f; 
		private float _thrustDeceleration = 5.0f; 

		private Line2D _trailLine;
		private int _maxTrailPoints = 1000;
		private ImageTexture _dashTexture;

		// СОСТОЯНИЯ АВТОПИЛОТА ДЛЯ HUD
		public bool IsAutopilotActive { get; private set; } = false;
		public string AutopilotExeState { get; private set; } = "OFF"; 
		public string TargetPlanetName { get; private set; } = "NONE";
		public float AutopilotTimer { get; private set; } = 0.0f;

		public SpaceGame.Entities.Planet.Planet TargetPlanetObj { get; private set; } = null;
		private float _landingThreshold = 1050f;
		private float _currentAutopilotSpeed = 0f; // Для плавного разгона автопилота

		[Signal] public delegate void CameraReadyEventHandler(Camera2D camera);

		public override void _Ready()
		{
			base._Ready();
			_shuttleCamera = GetNode<Camera2D>("%Camera2D");
			EmitSignal(SignalName.CameraReady, _shuttleCamera);

			_trailLine = GetNode<Line2D>("%TrailLine");
			_trailLine.TopLevel = true;

			Image img = Image.CreateEmpty(2, 1, false, Image.Format.Rgba8);
			img.SetPixel(0, 0, Colors.White);       
			img.SetPixel(1, 0, Colors.Transparent); 
			_dashTexture = ImageTexture.CreateFromImage(img);
			_trailLine.Texture = _dashTexture;
			_trailLine.TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled;
		}

		public override void _PhysicsProcess(double delta)
		{
			base._PhysicsProcess(delta);
			float fDelta = (float)delta;

			if (IsAutopilotActive && TargetPlanetObj != null)
			{
				ExecuteAutopilotLogic(fDelta);
			}
			else
			{
				ExecuteManualFlightLogic(fDelta);
			}

			if (_shuttleCamera != null)
			{
				_shuttleCamera.Zoom = _shuttleCamera.Zoom.Lerp(new Vector2(_targetZoom, _targetZoom), 3f * fDelta);
			}

			LinearVelocity = LinearVelocity.LimitLength(_maxVelocity);
			UpdateTrailEffect();
		}

		private void ExecuteManualFlightLogic(float fDelta)
		{
			Vector2 mousePos = GetGlobalMousePosition();
			Rotation = GlobalPosition.AngleToPoint(mousePos) + Mathf.Pi / 2f;

			float targetThrust = 0.0f;
			if (Input.IsMouseButtonPressed(MouseButton.Left)) targetThrust = 1.0f;

			float lerpSpeed = targetThrust > CurrentThrust ? _thrustAcceleration : _thrustDeceleration;
			CurrentThrust = Mathf.Lerp(CurrentThrust, targetThrust, lerpSpeed * fDelta);
			ApplyCentralForce(-Transform.Y * _enginePower * CurrentThrust);
		}

		private void ExecuteAutopilotLogic(float fDelta)
		{
			AutopilotTimer += fDelta;

			// АРХИТЕКТУРНЫЙ АПДЕЙТ: Если текущая планета УСПЕШНО ЗАХВАЧЕНА
			if (TargetPlanetObj.isCaptured)
			{
				GD.Print($"Цель {TargetPlanetName} захвачена. Автопилот ищет следующую планету сектора...");
				
				// Сбрасываем старые параметры, но НЕ выключаем IsAutopilotActive!
				TargetPlanetObj = null;
				TargetPlanetName = "NONE";
				_currentAutopilotSpeed = 0f;
				AutopilotTimer = 0.0f;
				
				// Мгновенно запускаем поиск новой незахваченной планеты
				FindNearestUncapturedPlanet();
				return;
			}

			Vector2 toPlanet = TargetPlanetObj.GlobalPosition - GlobalPosition;
			float distance = toPlanet.Length();

			// Стадия 1: ВЫБОР ЦЕЛИ (Задержка 1.5 сек перед каждым новым прыжком)
			if (AutopilotTimer < 1.5f)
			{
				AutopilotExeState = "SELECTING...";
				LinearVelocity = LinearVelocity.Lerp(Vector2.Zero, 3f * fDelta);
				_targetZoom = 1.0f; // Возвращаем камеру к норме во время расчетов
				return;
			}

			// Стадия 2: ПОЛЕТ К ЦЕЛИ

			// Стадия 2: ПОЛЕТ К ЦЕЛИ
			if (distance > _landingThreshold)
			{
				AutopilotExeState = "APPROACHING";
				_targetZoom = _minZoom;

				// БАЗОВЫЙ ВЕКТОР НАПРАВЛЕНИЯ К ЦЕЛИ
				Vector2 desiredDirection = toPlanet.Normalized();

				// --- СИСТЕМА НАВИГАЦИОННОГО ОБХОДА ПРЕГРАД (ОБЛЕТ ПО ДУГЕ) ---
				var allPlanets = GetTree().GetNodesInGroup("planets");
				Planet.Planet obstaclePlanet = null;
				float closestObstacleDist = float.MaxValue;

				foreach (Node node in allPlanets)
				{
					if (node is Planet.Planet planet && planet != TargetPlanetObj)
					{
						// Считаем вектор от корабля до этой чужой планеты
						Vector2 toObstacle = planet.GlobalPosition - GlobalPosition;
						float distToObstacle = toObstacle.Length();

						// Если планета находится близко и ПРЯМО НА НАШЕМ ПУТИ
						// Проверяем это через скалярное произведение (Dot Product) векторов
						if (distToObstacle < 4000f && desiredDirection.Dot(toObstacle.Normalized()) > 0.8f)
						{
							if (distToObstacle < closestObstacleDist)
							{
								closestObstacleDist = distToObstacle;
								obstaclePlanet = planet;
							}
						}
					}
				}

				// Если впереди обнаружена преграда — корректируем курс по дуге!
				if (obstaclePlanet != null)
				{
					Vector2 toObstacleVec = obstaclePlanet.GlobalPosition - GlobalPosition;
					Vector2 avoidanceDirection = new Vector2(-toObstacleVec.Y, toObstacleVec.X).Normalized();

					if (desiredDirection.Cross(toObstacleVec.Normalized()) > 0f)
					{
						avoidanceDirection = -avoidanceDirection;
					}

					float avoidanceWeight = Mathf.Clamp(1.0f - (closestObstacleDist / 4000f), 0.2f, 0.9f);
					desiredDirection = desiredDirection.Lerp(avoidanceDirection, avoidanceWeight).Normalized();
					
					AutopilotExeState = "CHASING"; // Включаем статус маневра

					// --- ФИЗИЧЕСКИЙ ФИКС: БОКОВЫЕ МАНЕВРОВЫЕ ДВИГАТЕЛИ ---
					// Чем ближе корабль к чужой планете, тем мощнее боковые двигатели 
					// будут ОТТАЛКИВАТЬ шаттл вбок (по вектору avoidanceDirection) подальше от коллизии!
					float lateralThrustPower = 1800f * (1.0f - (closestObstacleDist / 4000f));
					ApplyCentralForce(avoidanceDirection * lateralThrustPower);
				}


				// ПРИМЕНЯЕМ ПОЛУЧЕННЫЙ ВЕКТОР ТРАЕКТОРИИ
				// Поворачиваем нос корабля по скорректированной траектории дуги
				float targetAngle = desiredDirection.Angle() + Mathf.Pi / 2f;
				Rotation = targetAngle;

				CurrentThrust = Mathf.Lerp(CurrentThrust, 1.0f, _thrustAcceleration * fDelta);
				_currentAutopilotSpeed = Mathf.MoveToward(_currentAutopilotSpeed, _maxVelocity, 800f * fDelta);

				// Ведем корабль строго по дуге маневра
				LinearVelocity = desiredDirection * Mathf.Min(_currentAutopilotSpeed, distance * 1.2f);
			}


			else
			{
				// Стадия 3: МЯГКАЯ ПОСАДКА И СТЫКОВКА
				AutopilotExeState = "LANDED";
				_targetZoom = _maxZoom; // Максимальное приближение камеры при посадке!

				LinearVelocity = Vector2.Zero;
				AngularVelocity = 0f;
				CurrentThrust = 0f;

				Vector2 faceToSpace = (GlobalPosition - TargetPlanetObj.GlobalPosition).Normalized();
				Rotation = faceToSpace.Angle() + Mathf.Pi / 2f;

				// Передаем планете сигнал, что мы сидим на ней
				TargetPlanetObj.RegisterPlayerLanding(this);
			}
		}


		private void FindNearestUncapturedPlanet()
		{
			var nodes = GetTree().GetNodesInGroup("planets");
			Planet.Planet nearest = null;
			float minDistance = float.MaxValue;

			foreach (Node node in nodes)
			{
				if (node is Planet.Planet planet && !planet.isCaptured)
				{
					float dist = GlobalPosition.DistanceTo(planet.GlobalPosition);
					if (dist < minDistance)
					{
						minDistance = dist;
						nearest = planet;
					}
				}
			}

			if (nearest != null)
			{
				// ИСПРАВЛЕНО: Записываем найденный узел в TargetPlanetObj, 
				// чтобы World.cs смог увидеть координаты планеты и натянуть линию!
				TargetPlanetObj = nearest; 
				
				TargetPlanetName = nearest.PlanetName.ToUpper();
				IsAutopilotActive = true;
				AutopilotTimer = 0.0f;
				_currentAutopilotSpeed = LinearVelocity.Length();
				GD.Print($"Новая цель захвата определена: {TargetPlanetName}!");
			}
			else
			{
				// Если планет больше нет — вот теперь со спокойной душой отключаемся
				CancelAutopilot();
				GD.Print("ВСЕ ПЛАНЕТЫ СЕКТОРА ЗАХВАЧЕНЫ! Галактический доминион установлен.");
			}
		}


		public void CancelAutopilot()
		{
			IsAutopilotActive = false;
			TargetPlanetObj = null;
			TargetPlanetName = "NONE";
			AutopilotExeState = "OFF";
			
			// Принудительно возвращаем зум к 100% масштабу, 
			// но теперь один раз при отключении, а не каждый кадр!
			_targetZoom = 1.0f; 
			GD.Print("Автопилот отключен. Ручное управление восстановлено.");
		}


		public override void _UnhandledInput(InputEvent @event)
		{
			base._UnhandledInput(@event);

			if (@event is InputEventKey eventKey && eventKey.Keycode == Godot.Key.A && eventKey.IsPressed())
			{
				if (IsAutopilotActive) CancelAutopilot();
				else FindNearestUncapturedPlanet();
			}

			if (@event.IsActionPressed("wheel_up") || @event.IsActionPressed("wheel_down"))
			{
				if (IsAutopilotActive) CancelAutopilot();
				
				if (@event.IsActionPressed("wheel_up")) _targetZoom += _zoomSpeed;
				if (@event.IsActionPressed("wheel_down")) _targetZoom -= _zoomSpeed;
				_targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
			}
		}

		private void UpdateTrailEffect()
		{
			if (_trailLine == null) return;
			_trailLine.AddPoint(GlobalPosition);
			if (_trailLine.GetPointCount() > _maxTrailPoints) _trailLine.RemovePoint(0);

			float currentSpeed = LinearVelocity.Length();
			if (currentSpeed < 10f) { _trailLine.Visible = false; return; }
			_trailLine.Visible = true;

			float speedFactor = currentSpeed / _maxVelocity;
			_trailLine.Width = 2.0f; 
			Color trailColor = _trailLine.Modulate;
			trailColor.A = Mathf.Lerp(0.2f, 0.7f, speedFactor);
			_trailLine.Modulate = trailColor;
		}
	}
}
