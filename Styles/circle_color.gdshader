shader_type canvas_item;

uniform vec4 circle_color : source_color;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    COLOR = vec4(circle_color.rgb, tex.a * circle_color.a);
}