shader_type canvas_item;

// Нам больше НЕ нужны никакие параметры размеров экрана!
uniform vec2 camera_offset = vec2(0.0);
uniform vec2 camera_zoom = vec2(1.0);
uniform float grid_size = 100.0;
uniform vec4 grid_color : source_color = vec4(0.2, 0.2, 0.2, 1.0);
uniform float line_thickness = 1.0;

void fragment() {
    // 1. Берем координату текущего пикселя на экране (в диапазоне разрешения, например 1152x648)
    vec2 screen_pos = FRAGCOORD.xy;
    
    // 2. Получаем размер экрана из встроенной матрицы
    // SCREEN_PIXEL_SIZE равен vec2(1.0 / width, 1.0 / height)
	vec2 screen_size = 1.0 / SCREEN_PIXEL_SIZE; 

    
    // 3. Вычисляем мировые координаты пикселя. 
    // Центрируем экран, делим на зум и прибавляем смещение камеры.
    vec2 world_pos = ((screen_pos - (screen_size * 0.5)) / camera_zoom) + camera_offset;
    
    // 4. Отрисовка процедурной сетки
    vec2 grid = abs(mod(world_pos - (line_thickness * 0.5), grid_size));
    
    // Динамический антиалиасинг (стабилизация линий при зуме)
    vec2 unit_size = vec2(1.0 / camera_zoom.x);
    vec2 filter_width = max(vec2(line_thickness), unit_size);

    if (grid.x < filter_width.x || grid.y < filter_width.y) {
        COLOR = grid_color;
    } else {
        COLOR = vec4(0.08, 0.06, 0.13, 1.0); // Твой цвет космоса
    }
}
