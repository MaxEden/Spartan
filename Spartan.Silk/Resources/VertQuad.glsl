﻿#version 100
attribute vec2 Position;
attribute vec4 Color;
attribute vec2 Uv;

uniform mat4 World;

varying vec2 outUv;
varying vec4 outColor;

void main()
{
    vec4 pos2 = World * vec4(Position, 0.0, 1.0);
    gl_Position = pos2;  
    outColor = Color;
    outUv = Uv;
}
