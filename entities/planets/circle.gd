@tool
extends Line2D

@export var radius: float = 100.0:
	set(value):
		radius = value
		if is_inside_tree():
			update_circle()
			queue_redraw() # Заставляет перерисовать заливку

@export var segments: int = 64:
	set(value):
		segments = max(3, value)
		if is_inside_tree():
			update_circle()
			queue_redraw() # Заставляет перерисовать заливку

func _ready() -> void:
	joint_mode = Line2D.LINE_JOINT_ROUND
	begin_cap_mode = Line2D.LINE_CAP_ROUND
	end_cap_mode = Line2D.LINE_CAP_ROUND
	antialiased = true
	closed = true
	update_circle()

func update_circle() -> void:
	clear_points()
	for i in range(segments):
		var angle = (TAU / segments) * i
		var pos = Vector2(cos(angle), sin(angle)) * radius
		add_point(pos)

# Функция отвечает за рисование заливки внутри круга
func _draw() -> void:
	# Рисуем заполненный круг в центре (0, 0), с нужным радиусом и черным цветом
	draw_circle(Vector2.ZERO, radius, Color.BLACK)
