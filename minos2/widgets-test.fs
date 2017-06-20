\ simple tests for widgets code

\ Copyright (C) 2014,2016 Free Software Foundation, Inc.

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

require widgets.fs

also minos

glue*2 $FFFFFFDF 32e }}frame dup .button2 value f1
glue*2 $FF7FFFFF 32e }}frame dup .button3 simple[] value f2
glue*1 $FF5F5FFF 8e }}frame dup .button1 value f3a
glue*1 $5FFF5FFF 16e }}frame dup .button1 value f3b
glue*1 $5F5FFFFF 24e }}frame dup .button1 value f3c
glue*1 $5F5F5FFF 32e }}frame dup .button1 value f3d
glue*1 $FF7F7FFF 32e }}frame dup .button1 simple[] value f4
glue*1 $7FFF7FFF 8e  }}frame dup .button1 simple[] value f5
glue*2 $7FFFFFFF ' atlas-tex }}image dup .button2 simple[] value f6
edit new value t1
edit new value t2a
edit new value t2b
edit new value t2c
edit new value t2d
text new value t3
{{ {{
{{ f3a t2a }}z t2a edit[] dup value z2a
{{ f3b t2b }}z t2b edit[] dup value z2b
{{ f3c t2c }}z t2c edit[] dup value z2c
{{ f3d t2d }}z t2d edit[] dup value z2d
{{ {{ f1 t1 }}z t1 edit[] dup value z1 f2 t3 }}h box[] dup value h1
{{ f4 f5 }}h box[] dup value h2
}}v box[] dup value h3
f6 }}h box[] to top-widget

also freetype-gl
48e FConstant fontsize#
atlas fontsize#
[IFDEF] android
    "/system/fonts/DroidSans.ttf"
[ELSE]
    "/usr/share/fonts/truetype/LiberationSans-Regular.ttf"
    2dup file-status nip [IF]
	2drop "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf"
	2dup file-status nip [IF]
	    2drop "/usr/share/fonts/truetype/NotoSans-Regular.ttf"
	    2dup file-status nip [IF]
		2drop "/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf"
	    [THEN]
	[THEN]
    [THEN]
[THEN]
2dup file-status throw drop
open-font Value font1

atlas fontsize#
[IFDEF] android
    "/system/fonts/DroidSansFallback.ttf"
    2dup file-status nip [IF]
	2drop "/system/fonts/NotoSansSC-Regular.otf" \ for Android 6
	2dup file-status nip [IF]
	    2drop "/system/fonts/NotoSansCJK-Regular.ttc" \ for Android 7
	[THEN]
    [THEN]
[ELSE]
    "/usr/share/fonts/truetype/NotoSerifSC-Regular.otf"
    2dup file-status nip [IF]
	2drop "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc"
	2dup file-status nip [IF]
	    2drop "/usr/share/fonts/truetype/gkai00mp.ttf"
	    2dup file-status nip [IF]
		2drop "/usr/share/fonts/truetype/arphic-gkai00mp/gkai00mp.ttf"
	    [THEN]
	[THEN]
    [THEN]
[THEN]
2dup file-status throw drop
open-font Value font2
previous

: !t1 ( -- ) t1 >o
    "Dös isch a Tägscht!" font1 edit!  24e to border
    $884400FF to text-color o> ;

: !t2 ( -- )
    "混沌未分天地乱，茫茫渺渺无人见。" font2  t2a >o edit! 4e to border $001122DF to text-color o>
    "自从盘古破鸿蒙，开辟从兹清浊辨。" font2  t2b >o edit! 8e to border $221100DF to text-color o>
    "覆载群生仰至仁，发明万物皆成善。" font2  t2c >o edit! 12e to border $FFDDAADF to text-color o>
    "欲知造化会元功，须看西游释厄传。" font2  t2d >o edit! 16e to border $DDEEFFDF to text-color o> ;

: !t3 ( -- ) t3 >o
    "…" font1 text!  16e to border
    $00FF88FF to text-color o> ;

: htop-resize ( -- )
    !size 0e 1e dh* 1e dw* 1e dh* 0e resize ;
: !widgets ( -- ) !t1 !t2 !t3 top-widget .htop-resize ;

: widgets-test ( -- ) top-widget .widget-draw ;

also [IFDEF] android android [THEN]

: widgets-demo ( -- )  [IFDEF] hidekb  hidekb [THEN]  enter-minos
    1 level# +!  !widgets widgets-test  BEGIN  >looper
	[IFDEF] android  ?config-changer  [THEN]
	need-sync @ IF
	    top-widget .htop-resize  widgets-test  need-sync off  THEN
    level# @ 0= UNTIL  leave-minos ;

previous

script? [IF] widgets-demo bye [THEN]