using Godot;
using System;
using System.Collections.Generic;
using Vector2 = Godot.Vector2; // Разрешаем неоднозначность с векторами C#

namespace SpaceGame.Entities.Shuttle
{
	public partial class Shuttle : RigidBody2D
	{
		// ==========================================
		// 1. АРХИТЕКТУРА (СИГНАЛЫ)
		// ==========================================
		// Этот сигнал мы отправляем в пространство при старте.
		// Правило C# в Godot: имя делегата должно обязательно заканчиваться на EventHandler.
		[Signal]
		public delegate void CameraReadyEventHandler(Camera2D camera);

		// ==========================================
		// 2. НАСТРОЙКИ КОМПОНЕНТОВ И ФИЗИКИ
		// ==========================================
		// Настройки камеры игрока
		private Camera2D _shuttleCamera;
		private float _targetZoom = 1.0f;
		private float _zoomSpeed = 0.1f;
		private float _minZoom = 0.3f;
		private float _maxZoom = 2.0f;

		// Настройки двигателя и лимитов
		private float _maxVelocity = 5000f;
		private float _enginePower = 1500f;

		// Публичное свойство тяги (его будет читать наш HUD интерфейс)
		[Export]
		public float CurrentThrust { get; set; } = 0.0f; 
		
		private float _thrustAcceleration = 3.0f; // Скорость разгона реактора
		private float _thrustDeceleration = 5.0f; // Скорость затухания реактора

		// Настройки векторного следа
		private Line2D _trailLine;
		private int _maxTrailPoints = 1000;
		private ImageTexture _dashTexture;

		// ==========================================
		// 3. МЕТОД СТАРТА (_Ready)
		// ==========================================
		public override void _Ready()
		{
			base._Ready();

			// Находим локальную камеру внутри сцены шаттла
			_shuttleCamera = GetNode<Camera2D>("%Camera2D");

			// ГЛАВНОЕ ИЗМЕНЕНИЕ: Мы больше не ищем класс World и не просим его ни о чем!
			// Мы просто отправляем сигнал: «Я запустился, вот моя камера!»
			EmitSignal(SignalName.CameraReady, _shuttleCamera);

			// Настройка векторного следа
			_trailLine = GetNode<Line2D>("%TrailLine");
			_trailLine.TopLevel = true; // Отвязываем след от вращения носа корабля

			// Создаем пунктирную текстуру в памяти
			Image img = Image.CreateEmpty(2, 1, false, Image.Format.Rgba8);
			img.SetPixel(0, 0, Colors.White);       
			img.SetPixel(1, 0, Colors.Transparent); 
			
			_dashTexture = ImageTexture.CreateFromImage(img);
			_trailLine.Texture = _dashTexture;
			_trailLine.TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled;

			// ПОДПИСЫВАЕМСЯ НА СИГНАЛ СТОЛКНОВЕНИЯ ШАТТЛА
			this.BodyEntered += OnShuttleCollided;
		}

		// ==========================================
		// 4. МЕТОД КАЖДОГО КАДРА ФИЗИКИ (_PhysicsProcess)
		// ==========================================
		public override void _PhysicsProcess(double delta)
		{
			base._PhysicsProcess(delta);
			float fDelta = (float)delta;

			// 1. Поворот шаттла носом строго за курсором мыши
			Vector2 mousePos = GetGlobalMousePosition();
			float targetAngle = GlobalPosition.AngleToPoint(mousePos) + Mathf.Pi / 2f;
			Rotation = targetAngle;

			// 2. Логика плавного разгона инерции реактора (Нитро)
			float targetThrust = 0.0f;

			if (Input.IsMouseButtonPressed(MouseButton.Left))
			{
				targetThrust = 1.0f; // Если жмем ЛКМ — хотим 100% тяги
			}

			// Выбираем скорость изменения реактора (наращиваем или сбрасываем газ)
			float lerpSpeed = targetThrust > CurrentThrust ? _thrustAcceleration : _thrustDeceleration;
			
			// Плавно меняем текущую тягу от 0.0 до 1.0
			CurrentThrust = Mathf.Lerp(CurrentThrust, targetThrust, lerpSpeed * fDelta);

			// Физически толкаем корабль вперед с учетом плавной тяги
			ApplyCentralForce(-Transform.Y * _enginePower * CurrentThrust);

			// 3. Плавный зум камеры к желаемому масштабу колёсика
			if (_shuttleCamera != null)
			{
				float currentZoom = Mathf.Lerp(_shuttleCamera.Zoom.X, _targetZoom, 10f * fDelta);
				_shuttleCamera.Zoom = new Vector2(currentZoom, currentZoom);
			}

			// 4. Жесткий лимит максимальной скорости полета в космосе
			LinearVelocity = LinearVelocity.LimitLength(_maxVelocity);

			// 5. Отрисовка траектории полета
			UpdateTrailEffect();
		}

		// ==========================================
		// 5. МЕТОД ОТРИСОВКИ СЛЕДА (Внутренний)
		// ==========================================
		private void UpdateTrailEffect()
		{
			if (_trailLine == null) return;

			_trailLine.AddPoint(GlobalPosition);

			if (_trailLine.GetPointCount() > _maxTrailPoints)
			{
				_trailLine.RemovePoint(0);
			}

			float currentSpeed = LinearVelocity.Length();

			if (currentSpeed < 10f)
			{
				_trailLine.Visible = false;
				return;
			}
			_trailLine.Visible = true;

			float speedFactor = currentSpeed / _maxVelocity;
			_trailLine.Width = 2.0f; 

			Color trailColor = _trailLine.Modulate;
			trailColor.A = Mathf.Lerp(0.2f, 0.7f, speedFactor);
			_trailLine.Modulate = trailColor;
		}

		// ==========================================
		// 6. МЕТОД ОБРАБОТКИ ВВОДА КОЛЁСИКА МЫШИ
		// ==========================================
		public override void _UnhandledInput(InputEvent @event)
		{
			base._UnhandledInput(@event);

			if (@event.IsActionPressed("wheel_up"))
			{
				_targetZoom += _zoomSpeed;
			}

			if (@event.IsActionPressed("wheel_down"))
			{
				_targetZoom -= _zoomSpeed;
			}

			_targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
		}

		// ==========================================
		// 7. ON SHUTTLE COLLIDED
		// ==========================================
		private void OnShuttleCollided(Node body)
		{
			if (body is SpaceGame.Entities.Planet.Planet landedPlanet)
			{
				landedPlanet.RegisterPlayerLanding(this);
			}
		}
	}
}
