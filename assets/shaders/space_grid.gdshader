shader_type canvas_item;

uniform vec2 camera_offset = vec2(0.0);
uniform float grid_size = 1000.0;
uniform vec4 grid_color : source_color = vec4(0.2, 0.2, 0.2, 1.0);
uniform float line_thickness = 1.0;

// ДОБАВЛЯЕМ: Переменная для масштаба камеры (по умолчанию 1.0)
uniform vec2 camera_zoom = vec2(1.0);

void fragment() {
    // ТЕХНИЧЕСКИЙ ФИКС: Умножаем экранные координаты пикселя на зум камеры.
    // Благодаря этому при отдалении камеры линии сетки будут пропорционально сжиматься,
    // а при приближении — расширяться, сохраняя свои честные координаты в мире!
	// ИСПРАВЛЕНО: Теперь мы ДЕЛИМ на camera_zoom. 
	// При приближении (зум растет) ячейки будут честно увеличиваться.
	// При отдалении (зум падает) ячейки будут сжиматься вместе со всем миром!
	vec2 pixel_pos = (FRAGCOORD.xy - (1.0 / SCREEN_PIXEL_SIZE) / 2.0) / camera_zoom + camera_offset;

    
    // Проверяем, попадает ли пиксель на линию сетки
    vec2 grid = abs(mod(pixel_pos - line_thickness / 2.0, grid_size));
    
    if (grid.x < line_thickness || grid.y < line_thickness) {
        COLOR = grid_color;
    } else {
        COLOR = vec4(0.05, 0.0, 0.15, 1.0); // Синеватый космос
    }
}
