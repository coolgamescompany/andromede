using Godot;
using System;
using System.Numerics;
using Vector2 = Godot.Vector2;

public partial class Shuttle : RigidBody2D
{
	public override void _Ready()
	{
		base._Ready();
		// метод старта

		// 1. находим локальную камеру внутри сцены игрока (shuttle.tscn)
		Camera2D localCamera = GetNode<Camera2D>("%Camera2D");
		// 2. находим корневой узел сцены (скрипт World)
		World gameWorld = GetParent() as World;

		// если есть мир, то в метод закидываем наш узел камеры
		if (gameWorld != null)
		{
			gameWorld.RegisterCamera(localCamera);
		}

	}

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
	}
}
