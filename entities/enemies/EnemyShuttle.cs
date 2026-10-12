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
		private float _arrivalThreshold = 1050f;
		private bool _hasArrived = false;

		// READY
		public override void _Ready()
		{
			base._Ready();

			// настроим
		}

		// SET TARGET
		public void SetTarget(Vector2 pos)
		{
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

			// 3 если близко - стоп
			if (distance < _arrivalThreshold)
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


	}
}
