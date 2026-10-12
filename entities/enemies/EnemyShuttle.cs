using Godot;

namespace SpaceGame.Entities.Enemy
{
	public partial class EnemyShuttle : RigidBody2D
	{
		[Signal] public delegate void ArrivedAtTargetEventHandler();
		// ПЕРЕМЕННЫЕ
		private Vector2 _targetPosition;

		private float _maxVelocity = 500f;
		private float _acceleration = 800f;
		private float _currentSpeed = 0f;
		// private float _arrivalThreshold = 1050f;
		private bool _hasArrived = false;

		// READY
		public override void _Ready()
		{
			base._Ready();

			// настроим
		}

		// SET TARGET
		// Новое свойство для хранения ссылки на целевую планету
		public SpaceGame.Entities.Planet.Planet TargetPlanetObj { get; private set; } = null;

		// Обновленный метод установки цели, принимающий планету
		public void SetTarget(Vector2 pos, SpaceGame.Entities.Planet.Planet targetPlanet = null)
		{
			TargetPlanetObj = targetPlanet;
			
			if (_targetPosition.DistanceTo(pos) > 10f)
			{
				_targetPosition = pos;
				_hasArrived = false;
			}
		}


		// PHYSICS PROCESS
		public override void _PhysicsProcess(double delta)
		{
			float fDelta = (float)delta;

			// 1 вектор до цели
			Vector2 toTarget = _targetPosition - GlobalPosition;

			// 2 расстояние
			float distance = toTarget.Length();

			// Определяем порог прибытия: если летим к планете, берем её радиус + отступ. 
			// Если летим просто в точку (в строю за лидером), оставляем небольшой порог, например 150 пикселей.
			// Для врагов ставим порог: радиус планеты + 150 пикселей на габариты корпуса
			float currentThreshold = (TargetPlanetObj != null) ? (TargetPlanetObj.PlanetRadius + 60f) : 150f;



			// 3 если близко - стоп
			if (distance < currentThreshold)
			{
				LinearVelocity = Vector2.Zero;
				_currentSpeed = 0;

				if (!_hasArrived)
				{
					_hasArrived = true;
					EmitSignal(SignalName.ArrivedAtTarget);
				}

				return;
			}

			// 4 направление
			Vector2 direction = toTarget.Normalized();

			// 5 целевая скорость
			float targetSpeed = Mathf.Min(_maxVelocity, distance * 1.2f);

			// 6 плавно меняет _currentSpeed к targetSpeed
			_currentSpeed = Mathf.MoveToward(_currentSpeed, targetSpeed, _acceleration * fDelta);

			// 7 
			LinearVelocity = direction * _currentSpeed;

			// 8 
			Rotation = direction.Angle() + Mathf.Pi / 2f;
		}

		// TRY LAND ON PLANET
		public void TryLandOnPlanet(SpaceGame.Entities.Planet.Planet planet)
		{
			planet.RegisterLanding(this, isEnemy: true);
		}


	}
}
