 shader_type canvas_item;

uniform float blur_power: hint_range(0.0, 10.0, 0.01) = 0.0;
uniform float iterations: hint_range(1.0, 100.0, 1.0) = 50.0;

uniform sampler2D screen_texture: hint_screen_texture;

float hash12(vec2 x) {
	return fract(cos(mod(dot(x, vec2(13.9898, 8.141)), 3.14)) * 43758.5453);
}

void fragment() {
	vec2 uv = SCREEN_UV;
	float blur = blur_power * 0.05;
	vec4 sum = vec4(0.0);
	
	for(float i = 0.0; i < iterations; i++) {
		vec2 circle = vec2(cos(TAU * i / iterations), sin(TAU * i / iterations));
		float noise = hash12(vec2(i, uv.x + uv.y)) + blur;
		vec2 shift = circle * blur * noise;
		sum += texture(screen_texture, uv + shift);
	}
	
	sum /= iterations;
	COLOR = vec4(sum.rgb, sum.a);
}