using Godot;
using System;
using System.Collections.Generic;
using Vector2 = Godot.Vector2;

namespace SpaceGame.Entities.Shuttle
{
	public partial class Shuttle : RigidBody2D
	{
		// НАСТРОЙКИ КАМЕРЫ
		private Camera2D _shuttleCamera;
		private float _targetZoom = 1.0f;
		private float _zoomSpeed = 0.1f;
		private float _minZoom = 0.3f;
		private float _maxZoom = 2.0f;

		// НАСТРОЙКИ ШАТТЛА
		private float _maxVelocity = 5000f;
		private float _enginePower = 1500f;

		[Export] public float CurrentThrust { get; set; } = 0.0f; 
		private float _thrustAcceleration = 3.0f; 
		private float _thrustDeceleration = 5.0f; 

		// НАСТРОЙКИ СЛЕДА
		private Line2D _trailLine;
		private int _maxTrailPoints = 1000;
		private ImageTexture _dashTexture;

		// СОСТОЯНИЯ АВТОПИЛОТА
		public bool IsAutopilotActive { get; private set; } = false;
		private SpaceGame.Entities.Planet.Planet _targetPlanet = null;
		private float _landingThreshold = 1050f; // Физический радиус посадки (чуть больше радиуса планеты 1000)

		[Signal]
		public delegate void CameraReadyEventHandler(Camera2D camera);

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

			if (IsAutopilotActive && _targetPlanet != null)
			{
				ExecuteAutopilotLogic(fDelta);
			}
			else
			{
				ExecuteManualFlightLogic(fDelta);
			}

			// Плавный зум камеры к целевому масштабу
			if (_shuttleCamera != null)
			{
				float currentZoom = Mathf.Lerp(_shuttleCamera.Zoom.X, _targetZoom, 5f * fDelta);
				_shuttleCamera.Zoom = new Vector2(currentZoom, currentZoom);
			}

			LinearVelocity = LinearVelocity.LimitLength(_maxVelocity);
			UpdateTrailEffect();
		}

		// РУЧНОЙ РЕЖИМ ПОЛЕТА
		private void ExecuteManualFlightLogic(float fDelta)
		{
			// Поворот за мышкой
			Vector2 mousePos = GetGlobalMousePosition();
			float targetAngle = GlobalPosition.AngleToPoint(mousePos) + Mathf.Pi / 2f;
			Rotation = targetAngle;

			float targetThrust = 0.0f;
			if (Input.IsMouseButtonPressed(MouseButton.Left))
			{
				targetThrust = 1.0f;
			}

			float lerpSpeed = targetThrust > CurrentThrust ? _thrustAcceleration : _thrustDeceleration;
			CurrentThrust = Mathf.Lerp(CurrentThrust, targetThrust, lerpSpeed * fDelta);
			ApplyCentralForce(-Transform.Y * _enginePower * CurrentThrust);
		}

		// РЕЖИМ АВТОПИЛОТА (КИНЕМАТОГРАФИЧНЫЙ)
		private void ExecuteAutopilotLogic(float fDelta)
		{
			// Если кто-то захватил планету пока мы летели — сбрасываем цель
			if (_targetPlanet.isCaptured)
			{
				CancelAutopilot();
				return;
			}

			Vector2 toPlanet = _targetPlanet.GlobalPosition - GlobalPosition;
			float distance = toPlanet.Length();

			// 1. УПРАВЛЕНИЕ КАМЕРОЙ: Чем дальше планета, тем сильнее отдаляем обзор!
			// Если до планеты далеко, выставляем максимальное отдаление _minZoom (0.3)
			if (distance > 3000f)
			{
				_targetZoom = _minZoom;
			}
			else
			{
				// При подлете плавно возвращаем зум к стандартному (1.0)
				_targetZoom = Mathf.Lerp(_targetZoom, 1.0f, 2f * fDelta);
			}

			// 2. ДВИЖЕНИЕ К ЦЕЛИ
			if (distance > _landingThreshold)
			{
				// Смотрим строго на центр планеты
				float targetAngle = GlobalPosition.AngleToPoint(_targetPlanet.GlobalPosition) + Mathf.Pi / 2f;
				Rotation = targetAngle;

				// Разгоняем реактор на полную мощность (100% тяги)
				CurrentThrust = Mathf.Lerp(CurrentThrust, 1.0f, _thrustAcceleration * fDelta);
				
				// Толкаем корабль вперед к планете. Отключаем стандартную физику скольжения, 
				// плавно гася боковые сносы (честный полет по рельсам автопилота)
				LinearVelocity = toPlanet.Normalized() * Mathf.Min(_maxVelocity, distance * 1.5f);
			}
			else
			{
				// 3. ФИНАЛЬНАЯ ИДЕАЛЬНАЯ СТЫКОВКА (МЫ ПРИЛЕТЕЛИ!)
				LinearVelocity = Vector2.Zero;
				AngularVelocity = 0f;
				CurrentThrust = 0f;

				// Разворачиваем корабль спиной к планете (лицом в космос!)
				Vector2 faceToSpace = (GlobalPosition - _targetPlanet.GlobalPosition).Normalized();
				Rotation = faceToSpace.Angle() + Mathf.Pi / 2f;

				// Передаем планете сигнал, что мы успешно пристыковались
				_targetPlanet.RegisterPlayerLanding(this);
			}
		}

		// ПОИСК БЛИЖАЙШЕЙ НЕЗАХВАЧЕННОЙ ПЛАНЕТЫ
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
				_targetPlanet = nearest;
				IsAutopilotActive = true;
				GD.Print($"Автопилот активирован! Курс на планету: {_targetPlanet.PlanetName}. Дистанция: {Mathf.Round(minDistance)}");
			}
			else
			{
				GD.Print("В данном секторе нет незахваченных планет!");
			}
		}

		public void CancelAutopilot()
		{
			IsAutopilotActive = false;
			_targetPlanet = null;
			_targetZoom = 1.0f;
			GD.Print("Автопилот отключен. Ручное управление восстановлено.");
		}

		public override void _UnhandledInput(InputEvent @event)
		{
			base._UnhandledInput(@event);

			// Включение автопилота на физическую клавишу А
			if (@event is InputEventKey eventKey && eventKey.Keycode == Godot.Key.A && eventKey.IsPressed())
			{
				if (IsAutopilotActive)
				{
					CancelAutopilot();
				}
				else
				{
					FindNearestUncapturedPlanet();
				}
			}

			// Любой ручной щелчок колесика мыши мгновенно отключает автопилот
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
