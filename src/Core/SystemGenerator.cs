using Godot;
using System;
using System.Collections.Generic;

namespace SpaceGame.Core
{
	public partial class SystemGenerator
	{
		private readonly Node2D _parentWorld;
		private readonly PackedScene _planetTemplate;

		private int _totalPlanetsToSpawn = 10;
		private float _spawnRadiusMin = 15000f;
		private float _spawnRadiusMax = 100000f;

		public SystemGenerator(Node2D parentWorld, PackedScene planetTemplate)
		{
			_parentWorld = parentWorld;
			_planetTemplate = planetTemplate;
		}

		public void GenerateSystemPlanets()
		{
			if (_parentWorld == null) return;

			if (_planetTemplate == null)
			{
				GD.PrintErr("SYSTEM_GENERATOR_ERROR: PlanetTemplate is null!");
				return;
			}

			var rand = new Random();
			GD.Print($"--- STARTING PROCEDURAL GENERATION: {_totalPlanetsToSpawn} PLANETS ---");

			List<Vector2> spawnedPositions = new List<Vector2>();
			float minDistanceBetweenPlanets = 3500f;

			// Массив красивых неоновых цветов для атмосфер планет
			Color[] neonColors = new Color[] {
				Colors.Cyan,
				Colors.Magenta,
				Colors.Orange,
				Colors.Red,
				Colors.Purple,
				Colors.DeepSkyBlue,
				Colors.Yellow
			};

			// Массив возможных Sci-Fi классов планет
			string[] planetClasses = new string[] { "M-Class (Habitable)", "J-Class (Gas Giant)", "Ice-World", "Volcanic", "Desert-Class" };

			for (int i = 1; i <= _totalPlanetsToSpawn; i++)
			{
				var planetInstance = _planetTemplate.Instantiate<SpaceGame.Entities.Planet.Planet>();
				if (planetInstance != null)
				{
					planetInstance.isCaptured = false;
				}

				Vector2 spawnPosition = Vector2.Zero;
				bool isValidPosition = false;
				int attempts = 0;

				while (!isValidPosition && attempts < 100)
				{
					attempts++;
					float angle = (float)(rand.NextDouble() * Mathf.Tau);
					float distance = (float)(rand.NextDouble()) * (_spawnRadiusMax - _spawnRadiusMin) + _spawnRadiusMin;
					spawnPosition = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);

					isValidPosition = true;
					foreach (Vector2 existingPos in spawnedPositions)
					{
						if (spawnPosition.DistanceTo(existingPos) < minDistanceBetweenPlanets)
						{
							isValidPosition = false;
							break;
						}
					}
				}

				spawnedPositions.Add(spawnPosition);

				// РАЗНООБРАЗИЕ ПАРАМЕТРОВ:
				planetInstance.PlanetName = $"Sector-Star-PX-{i}";
				planetInstance.PlanetClass = planetClasses[rand.Next(planetClasses.Length)];
				
				// Радиус от 0.5 до 3.5, масса зависит от радиуса + небольшой случайный множитель
				planetInstance.PlanetRadius = 0.5f + (float)rand.NextDouble() * 3.0f;
				planetInstance.PlanetMass = planetInstance.PlanetRadius * (0.8f + (float)rand.NextDouble() * 0.4f);

				planetInstance.GlobalPosition = spawnPosition;
				planetInstance.ZIndex = -10;
				planetInstance.AddToGroup("planets");

				_parentWorld.AddChild(planetInstance);

				// СРАЗУ КРАСИМ АТМОСФЕРУ: Находим Line2D на свежесозданной планете и красим в случайный цвет
				var atmosphere = planetInstance.GetNodeOrNull<Line2D>("Line2D") ?? planetInstance.GetNodeOrNull<Line2D>("%Line2D");
				if (atmosphere != null)
				{
					atmosphere.DefaultColor = neonColors[rand.Next(neonColors.Length)];
				}

				GD.Print($"Planet {planetInstance.PlanetName} spawned smoothly.");
			}
			GD.Print("--- PROCEDURAL GENERATION COMPLETED ---");
		}




	}
}
