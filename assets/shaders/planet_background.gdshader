shader_type canvas_item;
render_mode blend_mix;

// ============================================================================
// Dark Space — процедурный фон главного меню.
//
// Рисует «космический пейзаж»: звёздное поле, лёгкую туманность и планету
// в стиле SpaceEngine. Планета — аналитическая сфера с FBM-континентами,
// терминатором, спекуляром по воде, атмосферным ореолом и облаками.
//
// Всё процедурно: ни одной текстуры. Цвета и параметры задаются из C#
// (MainMenu) на основе сгенерированной PlanetData.
// ============================================================================

uniform float aspect : hint_range(0.1, 10.0) = 1.6;
uniform vec2 planet_center = vec2(0.72, 0.0);
uniform float planet_radius : hint_range(0.02, 0.5) = 0.34;

uniform vec3 color_deep : source_color = vec3(0.02, 0.05, 0.12);
uniform vec3 color_land : source_color = vec3(0.30, 0.26, 0.20);
uniform vec3 color_high : source_color = vec3(0.60, 0.60, 0.62);
uniform vec3 color_atmosphere : source_color = vec3(0.20, 0.55, 1.0);
uniform vec3 color_cloud : source_color = vec3(0.90, 0.93, 1.0);

uniform float seed = 0.0;
uniform vec3 sun_dir = vec3(-0.55, 0.35, 0.75);
uniform float spin_speed = 0.05;
uniform float land_scale : hint_range(0.5, 8.0) = 3.0;
uniform float sea_level : hint_range(0.0, 1.0) = 0.5;
uniform float cloud_amount : hint_range(0.0, 1.0) = 0.25;
uniform float has_water : hint_range(0.0, 1.0) = 1.0;
uniform float gas_bands : hint_range(0.0, 1.0) = 0.0;
uniform float emissive : hint_range(0.0, 1.0) = 0.0;
uniform vec3 emissive_color : source_color = vec3(1.0, 0.35, 0.05);
uniform float star_amount : hint_range(0.0, 3.0) = 1.0;
uniform float brightness : hint_range(0.2, 2.0) = 1.0;

// ---------------------------------------------------------------------------
// Шум
// ---------------------------------------------------------------------------
float hash21(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

float hash13(vec3 p) {
	p = fract(p * 0.1031);
	p += dot(p, p.zyx + 31.32);
	return fract((p.x + p.y) * p.z);
}

float vnoise3(vec3 p) {
	vec3 i = floor(p);
	vec3 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);

	float n000 = hash13(i + vec3(0.0, 0.0, 0.0));
	float n100 = hash13(i + vec3(1.0, 0.0, 0.0));
	float n010 = hash13(i + vec3(0.0, 1.0, 0.0));
	float n110 = hash13(i + vec3(1.0, 1.0, 0.0));
	float n001 = hash13(i + vec3(0.0, 0.0, 1.0));
	float n101 = hash13(i + vec3(1.0, 0.0, 1.0));
	float n011 = hash13(i + vec3(0.0, 1.0, 1.0));
	float n111 = hash13(i + vec3(1.0, 1.0, 1.0));

	float nx00 = mix(n000, n100, f.x);
	float nx10 = mix(n010, n110, f.x);
	float nx01 = mix(n001, n101, f.x);
	float nx11 = mix(n011, n111, f.x);
	float nxy0 = mix(nx00, nx10, f.y);
	float nxy1 = mix(nx01, nx11, f.y);
	return mix(nxy0, nxy1, f.z);
}

float fbm3(vec3 p) {
	float value = 0.0;
	float amplitude = 0.5;
	for (int i = 0; i < 5; i++) {
		value += amplitude * vnoise3(p);
		p *= 2.03;
		amplitude *= 0.5;
	}
	return value;
}

// ---------------------------------------------------------------------------
// Звёздное поле
// ---------------------------------------------------------------------------
float star_layer(vec2 uv, float density, float size) {
	vec2 g = uv * density;
	vec2 id = floor(g);
	vec2 f = fract(g) - 0.5;
	float h = hash21(id + vec2(seed * 7.3, seed * 3.1));
	float present = step(0.972, h);
	float d = length(f);
	float core = smoothstep(size, 0.0, d);
	float twinkle = 0.55 + 0.45 * fract(h * 91.7);
	return present * core * twinkle;
}

void fragment() {
	vec2 uv = UV;

	// Ночной фон с лёгким градиентом и туманностью.
	vec3 col = vec3(0.008, 0.010, 0.024);
	float neb = fbm3(vec3(uv * vec2(aspect, 1.0) * 2.2, seed * 1.7));
	col += mix(vec3(0.02, 0.01, 0.05), vec3(0.01, 0.03, 0.06), neb) * (0.6 + 0.4 * neb);

	// Звёзды (три слоя разной плотности).
	float stars = 0.0;
	stars += star_layer(uv * vec2(aspect, 1.0), 26.0, 0.09);
	stars += star_layer(uv * vec2(aspect, 1.0) + 11.0, 44.0, 0.06) * 0.7;
	stars += star_layer(uv * vec2(aspect, 1.0) + 37.0, 80.0, 0.04) * 0.5;
	col += vec3(0.85, 0.92, 1.0) * stars * star_amount;

	// --- Планета -----------------------------------------------------------
	vec2 q = (uv - vec2(0.5)) * vec2(aspect, 1.0);
	vec2 p = q - planet_center;
	float r = planet_radius;
	float dist2 = dot(p, p) / (r * r);

	if (dist2 < 1.02) {
		float z = sqrt(max(0.0, 1.0 - dist2));
		vec3 n_geom = normalize(vec3(p.x / r, -p.y / r, z));

		// Вращение поверхности.
		float spin = TIME * spin_speed + seed;
		float cs = cos(spin);
		float sn = sin(spin);
		vec3 n_tex = vec3(cs * n_geom.x - sn * n_geom.z, n_geom.y, sn * n_geom.x + cs * n_geom.z);

		vec3 L = normalize(sun_dir);
		float diffuse = clamp(dot(n_geom, L), 0.0, 1.0);
		float terminator = smoothstep(0.0, 0.16, diffuse);
		float light = mix(0.06, 1.0, terminator) * brightness;

		// Поверхность.
		float continents = fbm3(n_tex * land_scale + vec3(seed));
		vec3 surface;
		float land_mask;
		if (gas_bands > 0.5) {
			// Газовый гигант: широтные пояса.
			float band = sin(n_tex.y * 9.0 + fbm3(n_tex * 2.0) * 3.0);
			float t = smoothstep(-0.5, 0.5, band);
			surface = mix(color_deep, color_land, t);
			surface = mix(surface, color_high, smoothstep(0.6, 0.95, continents));
			land_mask = 1.0;
		} else {
			land_mask = smoothstep(sea_level - 0.04, sea_level + 0.04, continents);
			land_mask = mix(land_mask, 1.0, 1.0 - has_water);
			surface = mix(color_deep, color_land, land_mask);
			surface = mix(surface, color_high, smoothstep(0.62, 0.92, continents) * land_mask);
		}

		// Облака.
		if (cloud_amount > 0.001) {
			float clouds = smoothstep(0.45, 0.75, fbm3(n_tex * (land_scale * 1.6) + vec3(TIME * 0.01, seed, 0.0)));
			surface = mix(surface, color_cloud, clouds * cloud_amount);
		}

		vec3 shaded = surface * light;

		// Спекуляр по воде.
		if (has_water > 0.5 && gas_bands < 0.5) {
			vec3 view = vec3(0.0, 0.0, 1.0);
			vec3 h = normalize(L + view);
			float spec = pow(max(dot(n_geom, h), 0.0), 48.0) * (1.0 - land_mask);
			shaded += vec3(spec) * 0.6;
		}

		// Эмиссия (лава / радиация / токсины).
		if (emissive > 0.001) {
			float veins = smoothstep(0.55, 0.75, fbm3(n_tex * (land_scale * 2.2) + vec3(seed * 2.0)));
			shaded += emissive_color * veins * emissive * (0.3 + 0.7 * (1.0 - land_mask));
		}

		// Атмосферный ореол (Fresnel).
		float fres = pow(1.0 - z, 3.0);
		shaded += color_atmosphere * fres * (0.25 + 0.75 * diffuse) * 0.9;

		// Сглаженный край диска.
		float edge = smoothstep(1.02, 0.985, dist2);
		col = mix(col, shaded, edge);
	}

	// Атмосферное свечение за диском.
	float atmo_ring = smoothstep(r * 0.98, r * 1.14, length(p)) * (1.0 - smoothstep(r * 1.14, r * 1.34, length(p)));
	vec3 L2 = normalize(sun_dir);
	col += color_atmosphere * atmo_ring * max(dot(vec2(L2.x, L2.y), normalize(p + vec2(0.0001))) * 0.5 + 0.5, 0.0) * 0.35;

	// Виньетка.
	float vig = smoothstep(1.25, 0.25, length((uv - 0.5) * vec2(aspect, 1.0)));
	col *= mix(0.6, 1.0, vig);

	COLOR = vec4(col, 1.0);
}
