shader_type canvas_item;

uniform vec2 camera_offset = vec2(0.0);
uniform float grid_size = 100.0;
uniform vec4 grid_color : source_color = vec4(0.2, 0.2, 0.2, 1.0);
uniform float line_thickness = 1.0;

void fragment() {
    // Рассчитываем координаты пикселя с учетом движения камеры
    vec2 pixel_pos = FRAGCOORD.xy + camera_offset;
    
    // Проверяем, попадает ли пиксель на линию сетки
    vec2 grid = abs(mod(pixel_pos - line_thickness / 2.0, grid_size));
    
    if (grid.x < line_thickness || grid.y < line_thickness) {
        COLOR = grid_color;
    } else {
        COLOR = vec4(0.05, 0.0, 0.15, 1.0); // Черный, синеватый космос
    }
}
