using Godot;
using System;

public partial class World : Node2D
{
	private ColorRect _spaceGrid;
	private Camera2D _camera;

	public override void _Ready()
	{
		base._Ready();
		GD.Print("Космос запущен!");
		_spaceGrid = GetNode<ColorRect>("%SpaceGrid");

		// Вместо прямых настроек используем безопасный отложенный вызов,
		// чтобы Godot успел правильно инициализировать узел.
		// "size" — это внутреннее имя свойства Size в движке.
		if (_spaceGrid != null)
		{
			// Берём реальный физический размер окна на твоём мониторе
			Vector2 viewportSize = GetViewportRect().Size;
			// Насильно растягиваем сетку под этот размер
			_spaceGrid.SetDeferred("size", viewportSize);
		}
	}

	// обновление каждый кадр
	public override void _Process(double delta)
	{
		base._Process(delta);

		if (_camera != null && _spaceGrid != null)
		{
			_spaceGrid.GlobalPosition = _camera.GlobalPosition - (_spaceGrid.Size / 2f);
			// 1. достаём материал сетки мира
			var shaderMaterial = _spaceGrid.Material as ShaderMaterial;
			// передаём глобальную позицию камеры в шейдер
			shaderMaterial?.SetShaderParameter("camera_offset", _camera.GlobalPosition);
		}
	}

	// Поиск камеры
	public void RegisterCamera(Camera2D playerCamera)
	{
		_camera = playerCamera;
		GD.Print("Камера игрока успешно зарегистрирована:", _camera);
	}

}
