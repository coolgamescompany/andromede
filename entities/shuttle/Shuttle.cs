using Godot;
using System;
using System.Numerics;
using Vector2 = Godot.Vector2;

public partial class Shuttle : RigidBody2D
{
	// НАСТРОЙКИ КАМЕРЫ
	private Camera2D _shuttleCamera;

	private float _targetZoom = 1.0f; // Текущий желаемый масштаб
	private float _zoomSpeed = 0.1f; // На сколько меняется масштаб за один щелчок
	private float _minZoom = 0.3f; // Максимальное отдаление 
	private float _maxZoom = 2.0f; // Максимальное приближение

	// READY
	public override void _Ready()
	{
		base._Ready();
		// метод старта

		// 1. находим локальную камеру внутри сцены игрока (shuttle.tscn)
		_shuttleCamera = GetNode<Camera2D>("%Camera2D");
		// 2. находим корневой узел сцены (скрипт World)
		World gameWorld = GetParent() as World;

		// если есть мир, то в метод закидываем наш узел камеры
		if (gameWorld != null)
		{
			gameWorld.RegisterCamera(_shuttleCamera);
		}

	}

	// PHYSICS PROCESS
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		// 1. Позиция курсора (мышки)
		Vector2 mousePos = GetGlobalMousePosition();
		Vector2 targetDirection = (mousePos - GlobalPosition).Normalized();
		float targetAngle = GlobalPosition.AngleToPoint(mousePos) + Mathf.Pi / 2f;
		Rotation = targetAngle;

		// 2. Проверяем нажатие ЛКМ (нитро)
		if (Input.IsMouseButtonPressed(MouseButton.Left))
		{
			// толкаем шаттл вперёд силой в 1500 единиц float
			ApplyCentralForce(-Transform.Y * 1500f);
		}

		// 3. Плавный зум камеры
		if (_shuttleCamera != null)
		{
			float currentZoom = Mathf.Lerp(_shuttleCamera.Zoom.X, _targetZoom, 10f * (float)delta);
			_shuttleCamera.Zoom = new Vector2(currentZoom, currentZoom);
		}
	}

	// UNHANDLED INPUT
	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);

		// если игрок крутит ВВЕРХ - приближение
		if (@event.IsActionPressed("wheel_up"))
		{
			_targetZoom += _zoomSpeed;
		}

		// если игрок крутит ВНИЗ - отдаление
		if (@event.IsActionPressed("wheel_down"))
		{
			_targetZoom -= _zoomSpeed;
		}

		// Ограничиваем желаемый зум в безопасных пределах
		_targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
	}

}
