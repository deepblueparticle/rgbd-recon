#version 430

noperspective in vec2 pass_TexCoord;
layout(pixel_center_integer) in vec4 gl_FragCoord;

uniform sampler2D texture_color;
uniform sampler2D texture_depth;
uniform vec2 resolution_inv;
uniform int lod;

uniform uvec2[20] texture_offsets;
uniform uvec2[20] texture_resolutions;

// currently does no apply here and does not work for SIDE by SIDE stereo
uniform vec2 viewport_offset;


out vec4 out_FragColor;
out float gl_FragDepth;

const int kernel_size = 4;

const float gauss_weights[16] = {
  0.4, 0.9, 0.9, 0.4,
  0.9, 1.8, 1.8, 0.9,
  0.9, 1.8, 1.8, 0.9,
  0.4, 0.9, 0.9, 0.4
};

ivec2 to_lod_pos(in vec2 pos, in int lod) {
  return ivec2(vec2(texture_offsets[lod]) + vec2(texture_resolutions[lod]) * pos); 
}

void main() {
  gl_FragDepth = 1.0f;
  // get normalized coordinates from integer fragcoord
  vec2 tex_coord = (gl_FragCoord.xy - vec2(texture_offsets[lod + 1])) / vec2(texture_resolutions[lod + 1]);
  ivec2 pos_int = ivec2(vec2(to_lod_pos(tex_coord, lod)) * vec2(2.0 / 3.0 , 1.0));

  float depth_av = 0.0;
  int num_samples = 0;
  vec4 samples[kernel_size * kernel_size];
  for(int x = 0; x < kernel_size; ++x) {
    for(int y = 0; y < kernel_size; ++y) {
      const ivec2 pos_tex = pos_int + ivec2(x- kernel_size * 0.5 + 1, y- kernel_size * 0.5 + 1);
      vec4 color = texelFetch(texture_color, pos_tex, 0);
      float depth = texelFetch(texture_depth, pos_tex, 0).r;
      if (color.a <= 0.0) {
        color.r = -1.0;
      }
      else {
        depth_av += depth;
        ++num_samples;
      }
      samples[x + y * kernel_size] = vec4(color.rgb, depth);
    }
  }

  if (num_samples == 0) {
    gl_FragDepth = texelFetch(texture_depth, pos_int, 0).r;
    if (gl_FragDepth < 1.0) {
      out_FragColor = vec4(0.0, 0.0, 0.0, -1.0);
    }
    else {
      out_FragColor = vec4(0.0, 1.0, 0.0, 0.0);
    }
    return;
  }

  depth_av /= float(num_samples);
  
  vec3 total_color = vec3(0.0);
  float total_depth = 0.0;
  float total_weight = 0.0;
  for(int i = 0; i < kernel_size * kernel_size; ++i) {
    if (samples[i].r >= 0.0) {
      if (samples[i].a >= depth_av) {
        float weight = 1.0;
        // float weight = gauss_weights[i];
        total_color += samples[i].rgb * weight;
        total_depth += samples[i].a * weight;
        total_weight += weight;
      }
    }
  }

  out_FragColor = vec4(total_color / total_weight, 1.0);
  gl_FragDepth = total_depth / total_weight;
  // out_FragColor = vec4(to_lod_pos(pass_TexCoord, lod), float(lod) / 9.0f, 1.0f);
}
