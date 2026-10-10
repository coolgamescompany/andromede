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
		private float _enginePower = 1500f; // Вынесли силу двигателя в переменную

		// ТЯГА ДВИГАТЕЛЯ (Публичное свойство C# с автоматическими get/set)
		// Экспортируем, чтобы при желании видеть значение в инспекторе Godot
		[Export]
		public float CurrentThrust { get; set; } = 0.0f; 
		
		private float _thrustAcceleration = 3.0f; // Скорость разгона реактора
		private float _thrustDeceleration = 5.0f; // Скорость затухания реактора

		// НАСТРОЙКИ СЛЕДА
		private Line2D _trailLine;
		private int _maxTrailPoints = 1000;
		private ImageTexture _dashTexture;

		// READY
		public override void _Ready()
		{
			base._Ready();

			_shuttleCamera = GetNode<Camera2D>("%Camera2D");
			World gameWorld = GetParent() as World;

			if (gameWorld != null)
			{
				gameWorld.RegisterCamera(_shuttleCamera);
			}

			_trailLine = GetNode<Line2D>("%TrailLine");
			_trailLine.TopLevel = true;

			Image img = Image.CreateEmpty(2, 1, false, Image.Format.Rgba8);
			img.SetPixel(0, 0, Colors.White);       
			img.SetPixel(1, 0, Colors.Transparent); 
			
			_dashTexture = ImageTexture.CreateFromImage(img);
			_trailLine.Texture = _dashTexture;
			_trailLine.TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled;
		}

		// PHYSICS PROCESS
		public override void _PhysicsProcess(double delta)
		{
			base._PhysicsProcess(delta);
			float fDelta = (float)delta;

			// 1. Поворот шаттла за мыжкой
			Vector2 mousePos = GetGlobalMousePosition();
			float targetAngle = GlobalPosition.AngleToPoint(mousePos) + Mathf.Pi / 2f;
			Rotation = targetAngle;

			// 2. МАТЕМАТИКА ПЛАВНОЙ ТЯГИ ДВИГАТЕЛЯ (Нитро)
			float targetThrust = 0.0f;

			if (Input.IsMouseButtonPressed(MouseButton.Left))
			{
				targetThrust = 1.0f; // Если жмем ЛКМ — стремимся к 100%
			}

			// Выбираем скорость изменения в зависимости от того, разгоняемся мы или тормозим
			float lerpSpeed = targetThrust > CurrentThrust ? _thrustAcceleration : _thrustDeceleration;
			
			// Плавно двигаем значение тяги к цели
			CurrentThrust = Mathf.Lerp(CurrentThrust, targetThrust, lerpSpeed * fDelta);

			// Прикладываем силу реактора, умноженную на текущую плавную тягу (от 0.0 до 1.0)
			ApplyCentralForce(-Transform.Y * _enginePower * CurrentThrust);

			// 3. Плавный зум камеры
			if (_shuttleCamera != null)
			{
				float currentZoom = Mathf.Lerp(_shuttleCamera.Zoom.X, _targetZoom, 10f * fDelta);
				_shuttleCamera.Zoom = new Vector2(currentZoom, currentZoom);
			}

			// 4. Лимит скорости полета
			LinearVelocity = LinearVelocity.LimitLength(_maxVelocity);

			// 5. Логика динамического следа
			UpdateTrailEffect();
		}

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
	}
}
