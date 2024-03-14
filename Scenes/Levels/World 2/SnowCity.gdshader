shader_type canvas_item;

uniform float speed = .075; // Speed of the scrolling effect
uniform vec2 direction = vec2(-1.0, 1.0); // Direction of scrolling, (-1, 1) for down-left

void fragment() {
    // Calculate the offset based on time, speed, and direction
    vec2 offset = direction * (TIME * speed);
    // Modulo operation to repeat the texture
    vec2 modulated_uv = mod(UV + offset, vec2(1.0, 1.0));
    // Sample the texture with the modulated UV coordinates
    COLOR = texture(TEXTURE, modulated_uv);
}
