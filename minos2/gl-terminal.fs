\ OpenGL terminal

\ Copyright (C) 2014,2015,2016 Free Software Foundation, Inc.

\ This file is part of Gforth.

\ Gforth is free software; you can redistribute it and/or
\ modify it under the terms of the GNU General Public License
\ as published by the Free Software Foundation, either version 3
\ of the License, or (at your option) any later version.

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.

\ You should have received a copy of the GNU General Public License
\ along with this program. If not, see http://www.gnu.org/licenses/.

\ opengl common stuff

\ :noname source type cr stdout flush-file throw ; is before-line

require gl-helper.fs

also [IFDEF] android android [THEN]

GL_FRAGMENT_SHADER shader: TerminalShader
#precision
uniform vec3 u_LightPos;        // The position of the light in eye space.
uniform sampler2D u_Texture;    // The input texture.
uniform float u_Ambient;        // ambient lighting level
uniform sampler2D u_Charmap;    // The character map
uniform sampler2D u_Colormap;   // the available colors
uniform vec2 u_texsize;         // the screen texture size
 
varying vec3 v_Position;        // Interpolated position for this fragment.
varying vec4 v_Color;           // This is the color from the vertex shader interpolated across the
                                // triangle per fragment.
varying vec3 v_Normal;          // Interpolated normal for this fragment.
varying vec2 v_TexCoordinate;   // Interpolated texture coordinate per fragment.
 
// The entry point for our fragment shader.
void main()
{
    // Will be used for attenuation.
    float distance = length(u_LightPos - v_Position);
 
    // Get a lighting direction vector from the light to the vertex.
    vec3 lightVector = normalize(u_LightPos - v_Position);
 
    // Calculate the dot product of the light vector and vertex normal. If the normal and light vector are
    // pointing in the same direction then it will get max illumination.
    float diffuse = max(dot(v_Normal, lightVector), 0.0);
 
    // Add attenuation.
    diffuse = diffuse * (1.0 / (1.0 + (0.10 * distance)));
 
    // Add ambient lighting
    diffuse = (diffuse * ( 1.0 - u_Ambient )) + u_Ambient;
 
    vec4 chartex = texture2D(u_Charmap, v_TexCoordinate);
    vec4 fgcolor = texture2D(u_Colormap, vec2(chartex.z, 0.));
    vec4 bgcolor = texture2D(u_Colormap, vec2(chartex.w, 0.));
    vec2 charxy = chartex.xy + vec2(0.0625, 0.125)*u_texsize*mod(v_TexCoordinate, 1.0/u_texsize);
    // mix background and foreground colors by character ROM alpha value
    // and multiply by diffuse
    vec4 pixel = texture2D(u_Texture, charxy);
    gl_FragColor = vec4(diffuse, diffuse, diffuse, 1.0)*(bgcolor*(1.0-pixel.a) + fgcolor*pixel.a);
    // gl_FragColor = diffuse*mix(bgcolor, fgcolor, pixel.a);
    // gl_FragColor = (v_Color * diffuse * pixcolor);
}

0 Value Charmap
0 Value Colormap
0 value texsize
0 Value terminal-program

: create-terminal-program ( -- program )
    ['] VertexShader ['] TerminalShader create-program ;

: terminal-init { program -- } program init
    program "u_Charmap\0" drop glGetUniformLocation to Charmap
    program "u_Colormap\0" drop glGetUniformLocation to Colormap
    program "u_texsize\0" drop glGetUniformLocation to texsize
    Charmap 1 glUniform1i
    Colormap 2 glUniform1i ;

tex: chars-tex
tex: video-tex
tex: color-tex

\ Variables and constants

[IFUNDEF] l, ' , Alias l, [THEN]

Create color-matrix \ vt100 colors
\ RGBA, but this is little endian, so write ABGR ,
$ff000000 l, \ Black
$ff3030ff l, \ Red
$ff20ff20 l, \ Green
$ff00ffff l, \ Yellow
$ffff6020 l, \ Blue - complete blue is too dark
$ffff00ff l, \ Magenta
$ffffff00 l, \ Cyan
$ffffffff l, \ White
$ff404040 l, \ dimm Black
$ff4040bf l, \ dimm Red
$ff40bf40 l, \ dimm Green
$ff40bfbf l, \ dimm Yellow
$ffbf4040 l, \ dimm Blue
$ffbf40bf l, \ dimm Magenta
$ffbfbf40 l, \ dimm Cyan
$ffbfbfbf l, \ dimm White

: term-load-textures ( addr u -- )
    chars-tex load-texture 2drop linear
    GL_TEXTURE2 glActiveTexture
    color-tex color-matrix $10 1 rgba-map nearest
    GL_TEXTURE0 glActiveTexture ;

Variable color-index
Variable err-color-index
bl dup $70 and 5 lshift or $F0F and 4 lshift
dup color-index ! err-color-index !
Variable std-bg

: ?default-fg ( n -- color ) dup 6 <= IF
	drop default-color fg>  THEN  $F xor ;
: ?default-bg ( n -- color ) dup 6 <= IF
	drop default-color bg>  THEN  $F xor ;
: fg! ( index -- )
    dup 0= IF  drop  EXIT  THEN  ?default-fg
    4 lshift color-index 2 + c! ;
: bg! ( index -- )
    dup 0= IF  drop  EXIT  THEN  ?default-bg
    4 lshift color-index 3 + c! ;
: err-fg! ( index -- ) ?default-fg
    4 lshift err-color-index 2 + c! ;
: err-bg! ( index -- ) ?default-bg
    4 lshift err-color-index 3 + c! ;
: bg>clear ( index -- ) $F xor
    $F and sfloats color-matrix +
    count s>f $FF fm/
    count s>f $FF fm/
    count s>f $FF fm/
    c@    s>f $FF fm/ glClearColor ;

: std-bg! ( index -- )  dup bg! dup std-bg ! bg>clear ;

: >extra-colors-bg ( -- ) >bg
    err-color  $F0F and over or to err-color
    info-color $F0F and over or to info-color
    warn-color $F0F and over or to warn-color drop ;

: >white White std-bg! White err-bg! Black fg! Red err-fg!
    White >extra-colors-bg White >bg Black >fg or to default-color ;
: >black Black std-bg! Black err-bg! White fg! Red err-fg!
    Black >extra-colors-bg Black >bg White >fg or to default-color ;

256 Value videocols
0   Value videorows
0   Value actualrows

2Variable gl-xy  0 0 gl-xy 2!
2Variable gl-wh 24 80 gl-wh 2!
Variable gl-lineend
Variable scroll-y
FVariable scroll-dest
FVariable scroll-source
FVariable scroll-time

\ HPM 20170317 
\   I like to set the number of columns by my own ;-) 
\   Earlier i stored my preferred values in hcols and vcols. 
\   Since the last update, scale-me calculates hcols and vcols dynamically. 
\   Therefore i added default-hcols and default-vcols and replaced 
\   the literals in scale-me by default-hcols and default-vcols. 
80 dup  Value default-hcols  Value hcols 
48 dup  Value default-vcols  Value hcols 

: form-chooser ( -- )
    screen-orientation 1 and  IF  hcols  ELSE  vcols  THEN
    dup dpy-h @ dpy-w @ 2* */ swap gl-wh 2! ;

: show-rows ( -- n ) videorows scroll-y @ - rows 1+ min ;
$20 Value minpow2#
: nextpow2 ( n -- n' )
    minpow2#  BEGIN  2dup u>  WHILE 2*  REPEAT  nip ;

: >rectangle ( -- )
    show-rows s>f rows fm/ -2e f* 1e f+
    >v
    -1e fover >xy n> v+
    -1e 1e >xy n> v+
    1e  1e >xy n> v+
    1e  fswap >xy n> v+ o> ;

: >texcoords ( -- )
    cols s>f videocols fm/  show-rows dup s>f nextpow2 dup fm/
    { f: tx f: ty }
    scroll-y @ over + videorows umin over - scroll-y @ - s>f fm/ fnegate
    { f: ox }
    >v
    0e ty ox f+ >st v+
    0e    ox    >st v+
    tx    ox    >st v+
    tx ty ox f+ >st v+ v> ;

0 Value videomem

\ : blank-screen ( -- )
\     color-index @ videomem videocols videorows * sfloats bounds ?DO
\ 	dup I l!
\     1 sfloats +LOOP  drop ;
\ blank-screen

: resize-screen ( -- )
    gl-xy @ 1+ actualrows max to actualrows
    gl-wh @ videocols u> gl-xy @ videorows u>= or IF
	videorows videocols * sfloats >r
	gl-wh @ nextpow2 videocols max to videocols
	gl-xy @ 1+ nextpow2 videorows max to videorows
	videomem videocols videorows * sfloats dup >r
	videorows sfloats + resize throw
	to videomem
	color-index @
	videomem r> r> /string bounds U+DO
	    dup I l!
	[ 1 sfloats ]L +LOOP drop
    THEN ;

2 sfloats buffer: texsize.xy

: draw-now ( -- )
    GL_TEXTURE1 glActiveTexture
    video-tex
    show-rows nextpow2 s>f  videocols s>f texsize.xy sf!+ sf!
    texsize 1 texsize.xy glUniform2fv
    show-rows nextpow2 >r
    videomem scroll-y @ r@ + videorows umin r@ -
    videocols * sfloats +
    videocols r> rgba-map wrap nearest

    v0 >rectangle >texcoords
    GL_TEXTURE0 glActiveTexture
    chars-tex
    i0 0 i, 1 i, 2 i, 0 i, 2 i, 3 i,
    GL_TRIANGLES draw-elements ;

: screen-scroll ( r -- )  fdup floor fdup f>s scroll-y ! f-
    f2* rows fm/ >y-pos  need-sync on ;

: gl-char' ( -- addr )
    gl-xy 2@ videocols * + sfloats videomem + ;

: gl-form ( -- h w ) gl-wh 2@ ;

Variable gl-emit-buf

: gl-cr ( -- )
    gl-lineend @ 0= IF
	gl-xy 2@ 1+ nip 0 swap gl-xy 2! THEN
    resize-screen  need-sync on  out off ;

: xchar>glascii ( xchar -- 0..7F )
    case
	'▄' of $0 endof
	'•' of 1 endof
	'°' of 2 endof
	'ß' of 3 endof
	'Ä' of 4 endof
	'Ö' of 5 endof
	'Ü' of 6 endof
	'ä' of 7 endof
	'ö' of 8 endof
	'ü' of 9 endof
	'µ' of 10 endof
	'✔' of 11 endof
	'✘' of 12 endof
	'▀' of $10 endof
	dup wcwidth -1 = IF  drop $7F
	ELSE  dup wcwidth 2 = IF  drop  13  ELSE  $7F umin  THEN
	THEN
    0 endcase ;

: gl-atxy ( x y -- )
    >r gl-wh @ min 0 max r> gl-xy 2!
    gl-xy cell+ @ out ! ;

: gl-at-deltaxy ( x y -- )
    >r s>d screenw @ sm/rem r> +
    gl-xy 2@ rot + >r + r> gl-atxy ;

: (gl-emit) ( char color -- )
    over 7 = IF  2drop  EXIT  THEN
    over #bs = IF  2drop -1 0 gl-at-deltaxy  EXIT  THEN
    over #lf = IF  2drop gl-cr  EXIT  THEN
    over #cr = IF  2drop gl-cr  EXIT  THEN
    over #tab = IF  >r drop bl gl-xy cell+ @ dup 1+ dfaligned swap - 0
    ELSE
	>r
	dup $7F u<= IF \ fast path for ASCII
	    xchar>glascii 1
	ELSE \ slow path for xchars
	    gl-emit-buf c$+!  gl-emit-buf $@ tuck
	    ['] x-size catch UTF-8-err = IF
		2drop $7F 1
	    ELSE  u< IF  rdrop  EXIT  THEN
		gl-emit-buf $@ drop ['] xc@ catch UTF-8-err =
		IF  drop $7F 1  ELSE  xchar>glascii
		    gl-emit-buf $@ ['] x-width catch UTF-8-err =
		    IF  2drop 1  THEN  abs
		THEN
	    THEN
	    gl-emit-buf $off
	THEN  $10
    THEN  { n m }

    n out +!
    resize-screen  need-sync on
    dup $70 and 5 lshift or $F0F and 4 lshift r> $FFFF0000 and or
    n 0 ?DO
	dup gl-char' l!
	gl-xy 2@ >r 1+ dup cols = dup gl-lineend !
	IF  drop 0 r> 1+ gl-xy 2! resize-screen
	ELSE  r> gl-xy 2!  THEN  m +
    LOOP  drop ;

: gl-emit ( char -- )  color-index @ (gl-emit) ;
: gl-emit-err ( char -- )
    dup (err-emit) \ errors also go to the error log
    err-color-index @ (gl-emit) ;
: gl-cr-err ( -- )
    #lf (err-emit)  gl-cr ;

: gl-type ( addr u -- )
    bounds ?DO  I c@ gl-emit  LOOP ;

: gl-type-err ( addr u -- )
    bounds ?DO  I c@ gl-emit-err  LOOP ;

: gl-page ( -- )  0 0 gl-atxy  0 to videorows  0 to actualrows
    0e screen-scroll  0e fdup scroll-source f! scroll-dest f!
    resize-screen need-sync on ;

: ?invers ( attr -- attr' ) dup invers and IF
    dup $F00 and 4 rshift over $F0 and 4 lshift or swap $7 and or  THEN ;
: >default ( attr -- attr' )
    dup  bg> 6 <= $F and >bg
    over fg> 6 <= $F and >fg or
    default-color -rot mux ;
: gl-attr! ( attribute -- )
    dup attr ! >default ?invers  dup bg> bg! fg> fg! ;
: gl-err-attr! ( attribute -- )
    dup attr ! >default ?invers  dup bg> err-bg! fg> err-fg! ;

4e FConstant scroll-deltat
: >scroll-pos ( -- 0..1 )
    ftime scroll-time f@ f- scroll-deltat f*
    1e fmin 0e fmax 0.5e f- pi f* fsin 1e f+ f2/ ;

: set-scroll ( r -- )
    scroll-y @ s>f y-pos sf@ f2/ rows fm* f+ scroll-source f!
    scroll-dest f!  ftime scroll-time f! ;

: scroll-slide ( -- )  scroll-dest f@ scroll-source f@ f= ?EXIT
    >scroll-pos fdup 1e f= IF  scroll-dest f@ scroll-source f!  THEN
    fdup scroll-dest f@ f* 1e frot f- scroll-source f@ f* f+ screen-scroll ;

: screen->gl ( -- )
    videomem 0= IF  resize-screen  THEN
    std-bg @ bg>clear clear
    terminal-program glUseProgram
    gl-char' 2 + dup be-uw@ swap le-w!
    draw-now
    gl-char' 2 + dup be-uw@ swap le-w!
    sync ;

: show-cursor ( -- )  need-show @ 0= ?EXIT
    rows ( kbflag @ IF  dup 10 / - 14 -  THEN ) >r
    gl-xy @ scroll-y @ dup r@ + within 0= IF
       gl-xy @ 1+ r@ - 0 max s>f set-scroll
    THEN  rdrop  need-show off ;

[IFUNDEF] win : win app window @ ; [THEN]

[IFDEF] android
    also jni
    JValue metrics \ screen metrics
    
    : >metrics ( -- )
	newDisplayMetrics dup to metrics
	clazz .getWindowManager .getDefaultDisplay .getMetrics ;
    
    : screen-wh ( -- rw rh )
	metrics ?dup-0=-IF  >metrics metrics  THEN >o
	widthPixels  xdpi 1/f fm* 25.4e f*      \ width in mm
	heightPixels ydpi 1/f fm* 25.4e f* o> ; \ height in mm
[ELSE]
    also x11
    : screen-wh ( -- rw rh )
	dpy XDefaultScreenOfDisplay >r
	r@ screen-mwidth  l@ s>f dpy-w @ r@ screen-width  l@ fm*/
	r@ screen-mheight l@ s>f dpy-h @ r> screen-height l@ fm*/ ;
[THEN]
previous

141e FValue default-diag \ Galaxy Note II is 80x48
1e FValue default-scale

: screen-diag ( -- rdiag )
    screen-wh f**2 fswap f**2 f+ fsqrt ;   \ diagonal in inch

: scale-me ( -- )
    \ smart scaler, scales using square root relation
    default-diag screen-diag f/ fsqrt default-scale f*
    \ HPM 20170314 
    \   replaced 80 by default-hcols and 48 by default-vcols defined earlier. 
    \   Now you can change the numbers of columns by changing 
    \   these default values and calling scale-me. 
    \   The dynamic calculation of hcols and vcols gives no warranty, 
    \   that hcols and vcols hit their default values exactly ;-) 
    1/f  default-hcols fdup fm* f>s to hcols  default-vcols fm* f>s to vcols 
    resize-screen config-changed ;

: gl-fscale ( f -- ) to default-scale scale-me ;
: gl-scale ( n -- ) s>f gl-fscale ;

: config-changer ( -- )
    getwh  >screen-orientation  form-chooser  scale-me  need-sync on ;
: ?config-changer ( -- )
    need-config @ 0> IF
	dpy-w @ dpy-h @ 2>r config-changer
	dpy-w @ dpy-h @ 2r> d<> IF  winch? on  need-config off
	ELSE  -1 need-config +!  THEN
    THEN ;

: screen-sync ( -- )  rendering @ -2 > ?EXIT \ don't render if paused
    ?config-changer
    need-sync @ win and level# @ 0<= and IF
	show-cursor screen->gl need-sync off  THEN ;

: >changed ( -- )
    config-change# need-config !
    BEGIN  >looper screen-sync need-config @ 0= UNTIL ;

: 1*scale   1 gl-scale ;
: 2*scale   2 gl-scale ;
: 4*scale   4 gl-scale ;

: scroll-yr ( -- float )  scroll-y @ s>f
    y-pos sf@ f2/ rows fm* f+ ;

: +scroll ( f -- f' )
    scroll-yr f+ actualrows 1 - s>f fmin
    0e fmax screen-scroll ;

: scrolling ( y0 -- )
    rows swap last-y0 motion-y0 ['] +scroll do-motion
    \ long? IF  kbflag @ IF  togglekb  THEN  THEN
    need-show off ;

#20. 2Value glitch#

: screen-slide ( -- )
    *input >r
    r@ IF
	r@ action @ \ dup -1 <> IF  dup .  THEN
	case
	    1 of
		r@ eventtime 2@ r@ eventtime' 2@ d- glitch# d>
		IF  ?toggle  THEN
		r@ action on  endof
	    3 of r@ action on  endof \ cancel
	    9 of r@ action on  endof \ hover
	    abs 1 <> IF  r@ y0 @ scrolling  
	    ELSE  last-y0 motion-y0 ['] +scroll drag-motion  THEN
	    0
	endcase
    THEN  rdrop ;

:noname ( flag -- flag ) level# @ 0> ?EXIT
    screen-sync screen-slide scroll-slide ; IS screen-ops

' gl-type     ' gl-emit     ' gl-cr     ' gl-form output: out>screen
' gl-type-err ' gl-emit-err ' gl-cr-err ' gl-form output: err>screen

out>screen
' gl-atxy IS at-xy
' gl-at-deltaxy IS at-deltaxy
' gl-page IS page
' gl-attr! IS attr!

err>screen
' gl-atxy IS at-xy
' gl-at-deltaxy IS at-deltaxy
' gl-page IS page
' gl-err-attr! IS attr!

default-out op-vector !

: >screen  err>screen op-vector @ debug-vector ! out>screen ;

\ initialize

: term-init ( -- )
    [IFDEF] clazz [ also jni ] ['] hideprog post-it [ previous ] [THEN]
    >screen-orientation
    create-terminal-program to terminal-program
    terminal-program terminal-init
    s" minos2/ascii.png" term-load-textures form-chooser
    unit-matrix MVPMatrix set-matrix  scale-me ;

:noname  defers window-init term-init config-changer ; IS window-init

>black \ make black default
\ >white \ make white default

window-init

previous previous \ remove opengl from search order

\ print system and sh outputs on gl terminal

0 warnings !@
: system ( addr u -- )
    r/o open-pipe throw 0 { fd w^ string }
    fd string $[]slurp string $[]. string $[]off ;
: sh '#' parse cr system ;
warnings !
