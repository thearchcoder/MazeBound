using UnityEngine;
using System.Collections.Generic;
using TMPro;

public enum MazeAxis { XPositive, XNegative, YPositive, YNegative, ZPositive, ZNegative }

public class MazeGenerator : MonoBehaviour {
	public static MazeGenerator instance;
	public int levelId = 1;
	public Material brick;

	private struct LevelConfig {
		public int width;
		public int height;
		public MazeAxis axis;
		public int ball_count;
		public Color[] ball_colors;
	}

	private Dictionary<int, LevelConfig> m_LevelConfigs;
	private int[,] m_Maze;
	private List<Vector3> m_PathPositions;
	private Vector3 m_WinningPosition;

	void Awake() {
		instance = this;
	}

	void LoadLevelFromFile(int level) {
		string filePath = Application.dataPath + "/Levels/" + level + ".txt";

		GameObject levelIdLabel = GameObject.FindGameObjectWithTag("LevelText");
		TextMeshProUGUI levelIdText = levelIdLabel.GetComponent<TextMeshProUGUI>();
		levelIdText.text = "Level " + level.ToString();

		if (!System.IO.File.Exists(filePath)) {
			Debug.LogError("Level file not found: " + filePath);
			return;
		}

		string[] lines = System.IO.File.ReadAllLines(filePath);

		int width = 0;
		int height = 0;
		Color[] ballColors = null;
		List<string> mazeLines = new List<string>();

		bool readingMaze = false;
		foreach (string line in lines) {
			if (line.StartsWith("WIDTH=")) {
				width = int.Parse(line.Substring(6));
			} else if (line.StartsWith("HEIGHT=")) {
				height = int.Parse(line.Substring(7));
			} else if (line.StartsWith("BALL_COLORS=")) {
				string colorData = line.Substring(12);
				string[] colorGroups = colorData.Split(';');
				ballColors = new Color[colorGroups.Length];
				for (int i = 0; i < colorGroups.Length; i++) {
					string[] rgb = colorGroups[i].Split(',');
					ballColors[i] = new Color(
						float.Parse(rgb[0]),
						float.Parse(rgb[1]),
						float.Parse(rgb[2])
					);
				}
			} else if (line.Trim().Length == 0 && !readingMaze) {
				continue;
			} else {
				readingMaze = true;
				mazeLines.Add(line);
			}
		}

		if (ballColors == null) {
			ballColors = new Color[] { new Color(1.0f, 0.4f, 0.4f) };
		}

		GenerateLevelFromText(width, height, ballColors, mazeLines.ToArray());
	}

	void GenerateLevelFromText(int width, int height, Color[] ballColors, string[] mazeLines) {
		LevelConfig config = new LevelConfig {
			width = width,
			height = height,
			axis = MazeAxis.ZPositive,
			ball_count = ballColors.Length,
			ball_colors = ballColors
		};

		m_Maze = new int[width, height];
		m_PathPositions = new List<Vector3>();

		List<Vector2Int> ballPositions = new List<Vector2Int>();
		List<Vector2Int> holePositions = new List<Vector2Int>();
		List<List<Vector2Int>> pressurePlates = new List<List<Vector2Int>>();
		List<List<Vector2Int>> gates = new List<List<Vector2Int>>();

		string gateSymbols = "!@$%^&*()";

		for (int y = 0; y < mazeLines.Length && y < height; y++) {
			string line = mazeLines[y];
			for (int x = 0; x < line.Length && x < width; x++) {
				char c = line[x];
				int gridY = height - 1 - y;

				if (c == '#') {
					m_Maze[x, gridY] = 1;
				} else if (c >= '1' && c <= '9') {
					int ballIndex = c - '1';
					while (ballPositions.Count <= ballIndex) {
						ballPositions.Add(new Vector2Int(-1, -1));
					}
					ballPositions[ballIndex] = new Vector2Int(x, gridY);
				} else if (c >= 'A' && c <= 'Z') {
					int holeIndex = c - 'A';
					while (holePositions.Count <= holeIndex) {
						holePositions.Add(new Vector2Int(-1, -1));
					}
					holePositions[holeIndex] = new Vector2Int(x, gridY);
				} else if (c >= 'a' && c <= 'z') {
					int plateIndex = c - 'a';
					while (pressurePlates.Count <= plateIndex) {
						pressurePlates.Add(new List<Vector2Int>());
					}
					pressurePlates[plateIndex].Add(new Vector2Int(x, gridY));
				} else if (gateSymbols.IndexOf(c) >= 0) {
					int gateIndex = gateSymbols.IndexOf(c);
					while (gates.Count <= gateIndex) {
						gates.Add(new List<Vector2Int>());
					}
					gates[gateIndex].Add(new Vector2Int(x, gridY));
				}
			}
		}

		BuildMaze(config);
		EnsureBallsExist(config);

		for (int i = 0; i < pressurePlates.Count; i++) {
			GameObject plateObj = null;
			if (pressurePlates[i].Count > 0) {
				Vector2Int platePos = pressurePlates[i][0];
				CreatePressurePlate(config, platePos.x, platePos.y, i, out plateObj);
			}

			if (i < gates.Count && gates[i].Count > 0 && plateObj != null) {
				foreach (Vector2Int gatePos in gates[i]) {
					CreateSingleBarrier(config, gatePos.x, gatePos.y, gates[i], i, plateObj);
				}
			}
		}

		for (int i = 0; i < holePositions.Count; i++) {
			if (holePositions[i].x >= 0) {
				CreateWinningArea(config, holePositions[i].x, holePositions[i].y, i);
			}
		}

		for (int i = 0; i < ballPositions.Count; i++) {
			if (ballPositions[i].x >= 0) {
				PositionBall(config, ballPositions[i].x, ballPositions[i].y, i);
			}
		}
	}

	void Start() {
		if (GameStateManager.instance != null)
		{
			levelId = GameStateManager.instance.currentLevel;
		}
		LoadLevelFromFile(levelId);
	}

	public void ReloadLevel(int level)
	{
		WinningArea.ResetBallCount();

		foreach (Transform child in transform)
		{
			Destroy(child.gameObject);
		}

		GameObject[] existingBalls = GameObject.FindGameObjectsWithTag("Ball");
		foreach (GameObject ball in existingBalls)
		{
			SwipeBall swipeBall = ball.GetComponent<SwipeBall>();
			if (swipeBall != null)
			{
				swipeBall.ResetBall();
			}

			Rigidbody rb = ball.GetComponent<Rigidbody>();
			if (rb != null)
			{
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
		}

		levelId = level;
		LoadLevelFromFile(level);
	}

	Color GetColorForIndex(int index) {
		Color[] colors = new Color[] {
			new Color(1.0f, 1.0f, 0.3f),  // a: yellow
			new Color(0.3f, 0.85f, 0.3f), // b: grass green
			new Color(0.3f, 0.7f, 1.0f),  // c: cyan
			new Color(0.9f, 0.3f, 1.0f),  // d: purple
			new Color(1.0f, 0.5f, 0.2f),  // e: orange
			new Color(1.0f, 0.3f, 0.5f),  // f: pink
			new Color(0.3f, 1.0f, 0.8f),  // g: mint
			new Color(0.8f, 0.8f, 0.3f),  // h: gold
			new Color(0.5f, 0.3f, 1.0f)   // i: violet
		};
		if (index < colors.Length) {
			return colors[index];
		}
		return new Color(1.0f, 1.0f, 1.0f); // white fallback
	}

	void CreatePressurePlate(LevelConfig config, int grid_x, int grid_y, int plateIndex, out GameObject plate) {
		float cell_size = 1.0f / Mathf.Max(config.width, config.height);
		float offset_x = -0.5f * config.width * cell_size;
		float offset_y = -0.5f * config.height * cell_size;
		Quaternion rotation = GetRotationFromAxis(config.axis);

		Vector3 local_pos = new Vector3(
			offset_x + (grid_x + 0.5f) * cell_size,
			offset_y + (grid_y + 0.5f) * cell_size,
			0.45f
		);
		Vector3 plate_position = rotation * local_pos;

		plate = new GameObject("PressurePlate");
		plate.transform.position = plate_position;
		plate.transform.rotation = rotation;
		plate.transform.parent = transform;

		float scaled_size = cell_size * 1.0f;
		Color plate_color = GetColorForIndex(plateIndex);
		Color plate_grayed = plate_color * 0.7f;

		GameObject outer_square = GameObject.CreatePrimitive(PrimitiveType.Cube);
		outer_square.name = "OuterSquare";
		outer_square.transform.parent = plate.transform;
		outer_square.transform.localPosition = Vector3.zero;
		outer_square.transform.localEulerAngles = new Vector3(90, 0, 0);
		outer_square.transform.localScale = new Vector3(scaled_size * 1.3f, scaled_size * 0.15f, scaled_size * 1.3f);
		Material outer_mat = new Material(Shader.Find("Unlit/Color"));
		outer_mat.color = plate_grayed;
		outer_square.GetComponent<Renderer>().material = outer_mat;
		Destroy(outer_square.GetComponent<BoxCollider>());

		GameObject inner_square = GameObject.CreatePrimitive(PrimitiveType.Cube);
		inner_square.name = "InnerSquare";
		inner_square.transform.parent = plate.transform;
		inner_square.transform.localPosition = new Vector3(0, 0, -0.0001f);
		inner_square.transform.localEulerAngles = new Vector3(90, 0, 0);
		inner_square.transform.localScale = new Vector3(scaled_size, scaled_size * 0.15f, scaled_size);
		Material inner_mat = new Material(Shader.Find("Unlit/Color"));
		inner_mat.color = plate_grayed * 0.65f;
		inner_square.GetComponent<Renderer>().material = inner_mat;
		Destroy(inner_square.GetComponent<BoxCollider>());

		SphereCollider trigger = plate.AddComponent<SphereCollider>();
		trigger.radius = scaled_size * 1.5f;
		trigger.isTrigger = true;

		plate.AddComponent<PressurePlate>();
	}

	void CreateSingleBarrier(LevelConfig config, int grid_x, int grid_y, List<Vector2Int> allGatePositions, int gateIndex, GameObject pressurePlate) {
		float cell_size = 1.0f / Mathf.Max(config.width, config.height);
		float offset_x = -0.5f * config.width * cell_size;
		float offset_y = -0.5f * config.height * cell_size;
		Quaternion rotation = GetRotationFromAxis(config.axis);

		Vector3 local_pos = new Vector3(
			offset_x + (grid_x + 0.5f) * cell_size,
			offset_y + (grid_y + 0.5f) * cell_size,
			0.45f
		);
		Vector3 barrier_position = rotation * local_pos;

		bool hasLeftWall = grid_x > 0 && m_Maze[grid_x - 1, grid_y] == 1;
		bool hasRightWall = grid_x < config.width - 1 && m_Maze[grid_x + 1, grid_y] == 1;
		bool hasTopWall = grid_y < config.height - 1 && m_Maze[grid_x, grid_y + 1] == 1;
		bool hasBottomWall = grid_y > 0 && m_Maze[grid_x, grid_y - 1] == 1;

		bool hasLeftGate = allGatePositions.Exists(pos => pos.x == grid_x - 1 && pos.y == grid_y);
		bool hasRightGate = allGatePositions.Exists(pos => pos.x == grid_x + 1 && pos.y == grid_y);
		bool hasTopGate = allGatePositions.Exists(pos => pos.x == grid_x && pos.y == grid_y + 1);
		bool hasBottomGate = allGatePositions.Exists(pos => pos.x == grid_x && pos.y == grid_y - 1);

		bool hasAnyWall = hasLeftWall || hasRightWall || hasTopWall || hasBottomWall;
		bool hasAnyGate = hasLeftGate || hasRightGate || hasTopGate || hasBottomGate;

		bool isHorizontal;
		if (hasAnyWall) {
			isHorizontal = (hasLeftWall || hasRightWall) && !hasTopWall && !hasBottomWall;
		} else if (hasAnyGate) {
			isHorizontal = (hasLeftGate || hasRightGate);
		} else {
			isHorizontal = false;
		}

		Vector3 barrierScale;
		if (isHorizontal) {
			barrierScale = new Vector3(cell_size, cell_size * 0.3f, cell_size * 3.0f);
		} else {
			barrierScale = new Vector3(cell_size * 0.3f, cell_size, cell_size * 3.0f);
		}

		GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
		barrier.name = "Barrier";
		barrier.transform.position = barrier_position;
		barrier.transform.rotation = rotation;
		barrier.transform.localScale = barrierScale;
		barrier.transform.parent = transform;

		Color gateBaseColor = GetColorForIndex(gateIndex);
		Color mazeBgColor = new Color(0.392f, 0.208f, 0.0f);
		Color finalGateColor = gateBaseColor * 0.5f + mazeBgColor * 0.5f;

		Material barrier_mat = new Material(Shader.Find("Unlit/Color"));
		barrier_mat.color = finalGateColor;
		barrier.GetComponent<Renderer>().material = barrier_mat;

		ControllableBarrier barrierScript = barrier.AddComponent<ControllableBarrier>();

		if (pressurePlate != null) {
			PressurePlate plateScript = pressurePlate.GetComponent<PressurePlate>();
			if (plateScript != null) {
				plateScript.onActivated.AddListener(barrierScript.Open);
				plateScript.onDeactivated.AddListener(barrierScript.Close);
			}
		}
	}

	void EnsureBallsExist(LevelConfig config) {
		GameObject[] existing_balls = GameObject.FindGameObjectsWithTag("Ball");

		if (existing_balls.Length > config.ball_count) {
			for (int i = config.ball_count; i < existing_balls.Length; i++) {
				Destroy(existing_balls[i]);
			}
		}
		else if (existing_balls.Length < config.ball_count) {
			if (existing_balls.Length == 0) {
				Debug.LogError("No balls found in scene. Add at least one ball with 'Ball' tag.");
				return;
			}

			GameObject original_ball = existing_balls[0];
			for (int i = existing_balls.Length; i < config.ball_count; i++) {
				GameObject new_ball = Instantiate(original_ball);
				new_ball.name = "Ball_" + i;
				new_ball.tag = "Ball";
			}
		}
	}

	void BuildMaze(LevelConfig config) {
		float cell_size = 1.0f / Mathf.Max(config.width, config.height);
		float offset_x = -0.5f * config.width * cell_size;
		float offset_y = -0.5f * config.height * cell_size;
		Quaternion rotation = GetRotationFromAxis(config.axis);

		for (int i = 0; i < config.width; i++) {
			for (int j = 0; j < config.height; j++) {
				Vector3 local_pos = new Vector3(
					offset_x + (i + 0.5f) * cell_size,
					offset_y + (j + 0.5f) * cell_size,
					0.45f
				);
				Vector3 rotated_pos = rotation * local_pos;

				if (m_Maze[i, j] == 1) {
					GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
					cube.transform.localScale = new Vector3(cell_size, cell_size, cell_size * 3.0f);
					cube.transform.rotation = rotation;
					cube.transform.position = rotated_pos;
					if (brick != null) cube.GetComponent<Renderer>().material = brick;
					cube.transform.parent = transform;

					float border_size = cell_size * 1.15f;
					GameObject border_cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
					border_cube.name = "WallBorder";
					border_cube.transform.localScale = new Vector3(border_size, border_size, cell_size * 3.0f);
					border_cube.transform.rotation = rotation;
					border_cube.transform.position = rotated_pos;
					border_cube.transform.parent = transform;

					Color mazeColor = new Color(0.494f, 0.263f, 0.0f);
					Material border_mat = new Material(Shader.Find("Unlit/Color"));
					border_mat.color = mazeColor * 0.8f;
					border_cube.GetComponent<Renderer>().material = border_mat;
					Destroy(border_cube.GetComponent<BoxCollider>());
				} else {
					m_PathPositions.Add(rotated_pos);
				}
			}
		}

		ScaleMazeBackground(config.width, config.height, cell_size);
	}

	void CreateWinningArea(LevelConfig config, int grid_x, int grid_y, int color_index) {
		float cell_size = 1.0f / Mathf.Max(config.width, config.height);
		float offset_x = -0.5f * config.width * cell_size;
		float offset_y = -0.5f * config.height * cell_size;
		Quaternion rotation = GetRotationFromAxis(config.axis);

		Vector3 local_pos = new Vector3(
			offset_x + (grid_x + 0.5f) * cell_size,
			offset_y + (grid_y + 0.5f) * cell_size,
			0.45f
		);
		m_WinningPosition = rotation * local_pos;

		GameObject win_area = new GameObject("WinningArea_" + color_index);
		win_area.tag = "WinningArea";
		win_area.transform.position = m_WinningPosition;
		win_area.transform.rotation = rotation;
		win_area.transform.parent = transform;

		float scaled_size = cell_size * 0.8f;
		Color area_color = config.ball_colors[color_index];
		Color grayed_color = area_color * 0.7f;

		GameObject inner_ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		inner_ring.name = "InnerRing";
		inner_ring.transform.parent = win_area.transform;
		inner_ring.transform.localPosition = new Vector3(0, 0, -0.0001f);
		inner_ring.transform.localEulerAngles = GetCylinderRotation(config.axis);
		float inner_diameter = scaled_size;
		float inner_thickness = scaled_size * 0.25f;
		inner_ring.transform.localScale = new Vector3(inner_diameter, inner_thickness, inner_diameter);
		Material inner_material = new Material(Shader.Find("Unlit/Color"));
		inner_material.color = grayed_color * 0.6f;
		inner_ring.GetComponent<Renderer>().material = inner_material;
		Destroy(inner_ring.GetComponent<CapsuleCollider>());

		GameObject outer_ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		outer_ring.name = "OuterRing";
		outer_ring.transform.parent = win_area.transform;
		outer_ring.transform.localPosition = Vector3.zero;
		outer_ring.transform.localEulerAngles = GetCylinderRotation(config.axis);
		float ring_diameter = scaled_size * 1.2f;
		float ring_thickness = scaled_size * 0.25f;
		outer_ring.transform.localScale = new Vector3(ring_diameter, ring_thickness, ring_diameter);
		Material outer_material = new Material(Shader.Find("Unlit/Color"));
		outer_material.color = grayed_color;
		outer_ring.GetComponent<Renderer>().material = outer_material;
		Destroy(outer_ring.GetComponent<CapsuleCollider>());

		SphereCollider trigger_collider = win_area.AddComponent<SphereCollider>();
		trigger_collider.radius = scaled_size * 0.8f;
		trigger_collider.isTrigger = true;

		WinningArea win_area_component = win_area.AddComponent<WinningArea>();
		win_area_component.requiredColor = area_color;
	}

	void PositionBall(LevelConfig config, int grid_x, int grid_y, int color_index) {
		GameObject[] existing_balls = GameObject.FindGameObjectsWithTag("Ball");
		if (existing_balls.Length <= color_index) return;

		GameObject ball = existing_balls[color_index];

		float cell_size = 1.0f / Mathf.Max(config.width, config.height);
		float offset_x = -0.5f * config.width * cell_size;
		float offset_y = -0.5f * config.height * cell_size;
		Quaternion rotation = GetRotationFromAxis(config.axis);

		Vector3 local_pos = new Vector3(
			offset_x + (grid_x + 0.5f) * cell_size,
			offset_y + (grid_y + 0.5f) * cell_size,
			0.45f
		);
		Vector3 ball_position = rotation * local_pos;

		ball.transform.position = ball_position;
		ball.transform.localScale = Vector3.one * cell_size * 0.95f;

		Color ball_color = config.ball_colors[color_index];
		Renderer ball_renderer = ball.GetComponent<Renderer>();
		if (ball_renderer != null) ball_renderer.material.color = ball_color;

		SwipeBall swipeBall = ball.GetComponent<SwipeBall>();
		if (swipeBall != null) swipeBall.ballColor = ball_color;

		Transform existingRing = ball.transform.Find("BallRing");
		if (existingRing != null)
		{
			Destroy(existingRing.gameObject);
		}

		Transform existingCore = ball.transform.Find("BallCore");
		if (existingCore != null)
		{
			Destroy(existingCore.gameObject);
		}

		GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		core.name = "BallCore";
		core.transform.parent = ball.transform;
		core.transform.localPosition = new Vector3(0, 0, -0.05f);
		core.transform.localRotation = Quaternion.identity;
		core.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);

		Material core_material = new Material(Shader.Find("Unlit/Color"));
		core_material.color = ball_color;
		core.GetComponent<Renderer>().material = core_material;
		Destroy(core.GetComponent<SphereCollider>());

		if (ball_renderer != null)
		{
			Material ring_material = new Material(Shader.Find("Unlit/Color"));
			ring_material.color = ball_color * 0.85f;
			ball_renderer.material = ring_material;
		}
	}

	void ScaleMazeBackground(int width, int height, float cell_size) {
		GameObject maze_bg = GameObject.FindGameObjectWithTag("MazeBG");
		if (maze_bg != null) {
			float maze_width = width * cell_size;
			float maze_height = height * cell_size;
			maze_bg.transform.localScale = new Vector3(maze_width, maze_height, maze_bg.transform.localScale.z);
			maze_bg.transform.localPosition = new Vector3(0, 0, 10);
		}
	}

	Quaternion GetRotationFromAxis(MazeAxis axis) {
		switch (axis) {
			case MazeAxis.XPositive: return Quaternion.Euler(0, 90, 0);
			case MazeAxis.XNegative: return Quaternion.Euler(0, -90, 0);
			case MazeAxis.YPositive: return Quaternion.Euler(-90, 0, 0);
			case MazeAxis.YNegative: return Quaternion.Euler(90, 0, 0);
			case MazeAxis.ZPositive: return Quaternion.identity;
			case MazeAxis.ZNegative: return Quaternion.Euler(0, 180, 0);
			default: return Quaternion.identity;
		}
	}

	Vector3 GetCylinderRotation(MazeAxis axis) {
		switch (axis) {
			case MazeAxis.XPositive:
			case MazeAxis.XNegative:
				return new Vector3(0, 0, 90);
			case MazeAxis.YPositive:
			case MazeAxis.YNegative:
				return Vector3.zero;
			case MazeAxis.ZPositive:
			case MazeAxis.ZNegative:
				return new Vector3(90, 0, 0);
			default:
				return Vector3.zero;
		}
	}
}
