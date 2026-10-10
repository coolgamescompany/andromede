using Godot;
using System;
using System.Numerics;
using System.Collections.Generic;
using Vector2 = Godot.Vector2;

namespace SpaceGame.Entities.Shuttle
{
	public partial class Shuttle : RigidBody2D
	{
		// НАСТРОЙКИ КАМЕРЫ
		private Camera2D _shuttleCamera; // Камера шаттла
		private float _targetZoom = 1.0f; // Текущий желаемый масштаб
		private float _zoomSpeed = 0.1f; // На сколько меняется масштаб за один щелчок
		private float _minZoom = 0.3f; // Максимальное отдаление 
		private float _maxZoom = 2.0f; // Максимальное приближение

		// НАСТРОЙКИ ШАТТЛА
		private float _maxVelocity = 5000f; // Максимальная скорость шаттла

		// НАСТРОЙКИ СЛЕДА
		private Line2D _trailLine;
		private int _maxTrailPoints = 1000; // Длина хвоста следа (количество точек)
		private ImageTexture _dashTexture; // Наша процедурная текстура пунктира

		// READY
		public override void _Ready()
		{
			base._Ready();
			// 1. находим локальную камеру внутри сцены игрока (shuttle.tscn)
			_shuttleCamera = GetNode<Camera2D>("%Camera2D");
			// 2. находим корневой узел сцены (скрипт World)
			World gameWorld = GetParent() as World;

			// если есть мир, то в метод закидываем наш узел камеры
			if (gameWorld != null)
			{
				gameWorld.RegisterCamera(_shuttleCamera);
			}

			// 3. находим узел следа
			_trailLine = GetNode<Line2D>("%TrailLine");
			// Отвязываем след от физического тела корабля. 
			// Если этого не сделать, след будет жестко крутиться вместе с носом шаттла
			_trailLine.TopLevel = true;
			// Генерируем пунктирную текстуру
			Image img = Image.CreateEmpty(2, 1, false, Image.Format.Rgba8);
			img.SetPixel(0, 0, Colors.White);       // 1-й пиксель: видимая линия
			img.SetPixel(1, 0, Colors.Transparent); // 2-й пиксель: пустота (промежуток)
			_dashTexture = ImageTexture.CreateFromImage(img);
			_trailLine.Texture = _dashTexture;
			_trailLine.TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled;


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

			// 4. Лимит скорости
			LinearVelocity = LinearVelocity.LimitLength(_maxVelocity);

			// 5. Логика динамического следа
			UpdateTrailEffect();
		}

		// UPDATE TRAIL EFFECT
		private void UpdateTrailEffect()
		{
			if (_trailLine == null) return;
			// Добавляем текущую глобальную позицию корабля в начало хвоста
			_trailLine.AddPoint(GlobalPosition);
			// Если точек стало больше лимита — удаляем самую старую точку с конца
			if (_trailLine.GetPointCount() > _maxTrailPoints)
			{
				_trailLine.RemovePoint(0);
			}

			// Рассчитываем текущую скорость корабля (от 0 до 5000)
			float currentSpeed = LinearVelocity.Length();

			// Если корабль стоит на месте, плавно скрываем след, чтобы не засорять экран
			if (currentSpeed < 10f)
			{
				_trailLine.Visible = false;
				return;
			}
			_trailLine.Visible = true;

			// МАТЕМАТИКА ДИНАМИЧЕСКОГО ПУНКТИРА:
			// Переводим скорость в коэффициент от 0.1 до 1.0
			float speedFactor = Mathf.Clamp(currentSpeed / _maxVelocity, 0.1f, 1.0f);
			// Чем выше скорость, тем МЕНЬШЕ масштаб повторения
			// В итоге: на малой скорости линии короткие и частые, на большой — длинные с большими дырами.
			float textureScale = 0.5f / speedFactor;
			// Вместо изменения Scale всего узла, мы меняем свойство TextureMode или шаг!
			// Но самый надежный способ для векторного стиля — просто менять ширину 
			// или частоту обновления точек. 
			// Давай пока оставим генерацию текстуры, но уберем строчку _trailLine.Scale = ...
			// Вместо неё мы будем динамически менять ширину следа в зависимости от скорости, 
			// чтобы он выглядел как «лазерный луч» при разгоне
			_trailLine.Width = 2.0f * speedFactor + 1.0f; 
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

}
