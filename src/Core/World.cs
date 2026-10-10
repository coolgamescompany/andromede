using Godot;
using System;

public partial class World : Node2D
{
	private ColorRect _spaceGrid;
	private Camera2D _camera;

	// READY
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

	// PROCESS
	public override void _Process(double delta)
	{
		base._Process(delta);

		if (_camera != null && _spaceGrid != null)
		{
			// 1. Базовый размер экрана
			Vector2 baseScreenSize = GetViewportRect().Size;

			// 2. Рассчитываем необходимый размер холста с учётом камеры
			// Если камера отдаляется (значение Zoom уменьшается), мы делим базовый размер экрана
			// на текущий зум камеры, чтобы физически увеличить прямоугольник сетки!
			Vector2 requiredSize = baseScreenSize / _camera.Zoom;
			// добавляем небольшой запас в 20%, чтобы края сетки гарантированно не вылезали на углах при поворотах
			_spaceGrid.Size = requiredSize * 1.2f;

			// 3. Центрируем холст сетки строго по камере, учитывая его НОВЫЙ динамический размер
			_spaceGrid.GlobalPosition = _camera.GlobalPosition - (_spaceGrid.Size / 2f);

			// 4. достаём материал сетки мира
			var shaderMaterial = _spaceGrid.Material as ShaderMaterial;
			// передаём глобальную позицию камеры в шейдер
			shaderMaterial?.SetShaderParameter("camera_offset", _camera.GlobalPosition);
			// Отправляем текущий вектор зума камеры в шейдер
			shaderMaterial?.SetShaderParameter("camera_zoom", _camera.Zoom);
		}
	}

	// Поиск камеры
	// REGISTER CAMERA
	public void RegisterCamera(Camera2D playerCamera)
	{
		_camera = playerCamera;
		GD.Print("Камера игрока успешно зарегистрирована:", _camera);
	}

}
