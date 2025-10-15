shader_type canvas_item;

uniform float fill_ratio : hint_range(0.0, 1.0) = 1.0;

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);

    // Keep only the bottom portion according to fill_ratio
    if (UV.x < (1.0 - fill_ratio)) {
        discard;
    }

    COLOR = tex_color;
}
