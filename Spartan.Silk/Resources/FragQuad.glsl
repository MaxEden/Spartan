precision mediump float;

varying vec2 outUv;
varying vec4 outColor;

uniform sampler2D uTexture;

void main()
{
    vec4 color = texture2D(uTexture, outUv);
    gl_FragColor =  outColor*color;
}
