using Godot;
using System;
using Vector2 = Godot.Vector2;

namespace SpaceGame.UI
{
	public partial class HudController : Control
	{
		// Ссылки на текстовые блоки
		private Label _systemInfoLabel;
		private Label _flightDataLabel;

		private RigidBody2D _shuttle;

		// READY
		public override void _Ready()
		{
			base._Ready();

			_systemInfoLabel = GetNode<Label>("%SystemInfoLabel");
			_flightDataLabel = GetNode<Label>("%FlightDataLabel");
		}

		// Метод, который мир или шаттл вызовет при старте, чтобы подключить приборы к кораблю
		// INITIALIZE
		public void Initialize(RigidBody2D shuttle)
		{
			_shuttle = shuttle;
		}

		// PROCESS
		public override void _Process(double delta)
		{
			base._Process(delta);

			if (_shuttle == null) return;

			UpdateSystemInfo();
			UpdateFlightData();
		}

		// UPDATE SYSTEM INFO
		private void UpdateSystemInfo()
		{
			// Пока зашьем эти данные жестко как константы, 
			// но архитектура готова к тому, чтобы в будущем брать их из генератора планет
			string model = "SHUTTLE-MK4";
			string starName = "Kepler-452";
			string starClass = "G2V";
			string radius = "1.11 R☉";
			string mass = "1.03 M☉";
			int capturedBodies = 0;
			int totalBodies = 5;

			_systemInfoLabel.Text =
				$"MODEL: {model}\n" +
				$"SECTOR STAR: {starName}\n" +
				$"STAR CLASS: {starClass}\n" +
				$"RADIUS: {radius}\n" +
				$"MASS: {mass}\n" +
				$"BODIES: {capturedBodies}/{totalBodies}";

		}

		// UPDATE FLIGHT DATA
		private void UpdateFlightData()
		{
			// 1. Рассчитываем координаты (POS)
			Vector2 pos = _shuttle.GlobalPosition;

			// 2. Рассчитываем скорость (VEL). У RigidBody2D скорость хранится в LinearVelocity.
			// Считаем общую скорость корабля (длину вектора) и округляем её
			float velocityMagnitude = _shuttle.LinearVelocity.Length();

			// 3. Рассчитываем тягу нитро (THR)
			// Если зажат ЛКМ — тяга 100%, иначе 0%
			int thrust = Input.IsMouseButtonPressed(MouseButton.Left) ? 100 : 0;

			// 4. Задел на будущее (ALT / GRAVITY)
			float altitude = 0.0f; 
			float gravity = 0.0f;

			_flightDataLabel.Text = 
				$"THR: {thrust}%\n" +
				$"POS: X:{Mathf.Round(pos.X)} Y:{Mathf.Round(pos.Y) * -1f}\n" +
				$"VEL: {Mathf.Round(velocityMagnitude)} km/h\n" +
				$"ALT: {altitude:F1}\n" +
				$"GRV: {gravity:F2}g";
		}

	}
}
