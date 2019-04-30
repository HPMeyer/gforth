\ MINOS2 markdown viewer

\ Copyright (C) 2019 Free Software Foundation, Inc.

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

\ Inspiration: wf.fs, a markdown-like parser, which converts to HTML

require jpeg-exif.fs

get-current also minos definitions

Defer .char

Variable md-text$
Variable preparse$
Variable last-cchar
Variable last-emph-flags
Variable emph-flags \ emphasis flags
Variable up-emph
Variable count-emph
Variable us-state

: reset-emph ( -- )
    last-emph-flags off
    last-cchar off
    emph-flags off
    up-emph off
    count-emph off
    us-state off ;

0 Value p-box \ paragraph box
0 Value v-box \ vertical box

[IFUNDEF] bits:
    : bit ( n "name" -- n*2 )   dup Constant 2* ;
    : bits: ( start n "name1" .. "namen" -- )
	0 ?DO bit LOOP drop ;
[THEN]

1 8 bits: italic underline 2underline sitalic bold mono strikethrough #dark-blue

: +emphs ( flags -- )
    \regular \sans
    dup [ underline 2underline or ]L and 2/  us-state !
    dup strikethrough and 4 rshift us-state +!
    dup mono and IF  \mono  THEN
    dup #dark-blue and IF  dark-blue  ELSE  blackish  THEN
    [ italic sitalic bold or or ]L and
    dup 1 and swap 3 rshift xor
    case
	1 of  \italic       endof
	2 of  \bold         endof
	3 of  \bold-italic  endof
    endcase ;

: md-text+ ( -- )
    md-text$ $@len IF  bl md-text$ c$+!  THEN ;
glue new Constant glue*\\
glue*\\ >o 0e 0g 1fill hglue-c glue! 0glue dglue-c glue! 1glue vglue-c glue! o>
: .\\ ( -- )
    glue*\\ }}glue p-box .child+ x-baseline p-box .parent-w >o to baseline' o> ;
: +p-box ( -- )
    {{ }}p box[] >bl dup v-box .child+
    dup >o "p-box" to name$ o>
    dup .subbox >o to parent-w "subbox" to name$ o o> box[] to p-box ;
: .md-text ( -- )
    md-text$ $@len IF
	us-state @ md-text$ $@ }}text-us p-box .child+ md-text$ $free
    THEN ;

: /source ( -- addr u )
    source >in @ safe/string ;

: +link ( o -- o )
    /source IF  c@ '(' =  IF  1 >in +! ')' parse link[]  THEN
    ELSE  drop  THEN ;

: jpeg? ( addr u -- flag )
    dup 4 - 0 max safe/string ".jpg" str= ;
: img-orient? ( addr u -- flag )
    2dup jpeg? IF
	>thumb-scan  img-orient 1- 0 max
    ELSE  2drop 0  THEN ;

Variable imgs#
-1 Value imgs#max

: load/thumb { w^ fn$ -- w h res flag }
    imgs# @ imgs#max u>=
    fn$ $@ jpeg? IF  thumbnail@ nip 0<> and  THEN
    IF
	thumbnail@ load-thumb  fn$ $free  true
    ELSE
	tex-xt dup >r image-tex[] >stack r@ execute
	fn$ @ image-file[] >stack
	fn$ $@ slurp-file mem>texture r> false
    THEN  1 imgs# +! ;

: wh>glue ( w h w% h% -- glue ) { f: w% f: h% }
    2dup dpy-h @ s>f fm/ h% f* dpy-w @ s>f fm/ w% f* fmin
    \ not bigger than x% of screen
    glue new >o fdup fm* vglue-c df!  fm* hglue-c df!  o o> ;

: }}image-file' ( addr u hmax vmax -- o ) { | w^ fn$ }
    file>fpath fn$ $!
    fn$ $@ img-orient? { img-rot# }
    fn$ @ load/thumb 2swap
    img-rot# 1 and IF  swap  THEN
    imgs# @ imgs#max u>  IF  15% f* fswap 15% f* fswap  THEN  wh>glue
    -rot IF  }}thumb  ELSE  white# }}image  THEN
    >o img-rot# to rotate# o o>  exif-close ;
: +image ( o -- o )
    /source IF  c@ '(' =  IF  1 >in +! ')' parse
	    2dup "file:" string-prefix? IF  5 /string
	    ELSE
		2dup "http:" string-prefix? >r
		2dup "https:" string-prefix? r> or IF  link[]  EXIT  THEN
	    THEN
	    50% 100% }}image-file'
	    >r {{ glue*l }}glue r> glue*l }}glue }}v box[]
	    swap .dispose-widget THEN
    ELSE  drop  THEN ;

: >lhang ( o -- o )
    p-box .parent-w >o dup to lhang o> ;

: default-char ( char -- )
    emph-flags @ last-emph-flags @ over last-emph-flags ! <> IF
	.md-text emph-flags @ +emphs
    THEN
    md-text$ c$+!  last-cchar off ;

: wspace ( -- ) ' ' xemit ;
: wspaces ( n -- ) 0 ?DO wspace LOOP ;

' default-char is .char

Create do-char $100 0 [DO] ' .char , [LOOP]

: md-char ( xt "char" -- )
    source >in @ /string drop c@ cells do-char + !  1 >in +! ;
: md-char: ( "char" -- )
    depth >r :noname depth r> - 1- roll md-char ;

: ?count-emph ( flag char -- )
    last-cchar @ over last-cchar ! <> IF  count-emph off
	emph-flags @ and 0= up-emph !
    ELSE  1 count-emph +!  drop  THEN ;

: render-line ( addr u attr -- )
    \G render a line
    emph-flags @ >r dup emph-flags ! +emphs
    [: BEGIN  /source  WHILE  1 >in +!
		c@ dup cells do-char + perform
	REPEAT  drop ;] execute-parsing
    r> emph-flags ! ;

: ]-parse ( -- addr u )
    /source drop
    BEGIN  ']' parse  dup IF  2dup + 1- c@ '\' =  ELSE  false  THEN  WHILE
	    2drop  REPEAT  + over - ;

Vocabulary md-tokens

md-char: * ( char -- )
    [ sitalic bold or ]L swap ?count-emph
    sitalic up-emph @ 0= IF  negate  THEN  emph-flags +! ;
md-char: _ ( char -- )
    [ italic underline 2underline or or ]L swap ?count-emph
    italic up-emph @ 0= IF  negate  THEN  emph-flags +! ;
md-char: ` ( char -- )
    mono swap ?count-emph
    mono up-emph @ 0= IF  negate  THEN  emph-flags +! ;
md-char: ~ ( char -- )
    strikethrough swap ?count-emph
    /source "~" string-prefix? IF
	1 >in +!
	strikethrough up-emph @ 0= IF  negate  THEN  emph-flags +!
    ELSE  '~' .char  THEN ;
md-char: \ ( char -- )
    drop /source IF  c@ .char  1 >in +!  ELSE  drop  THEN ;
md-char: ! ( char -- )
    /source "[" string-prefix? IF
	drop 1 >in +! ]-parse
	.md-text dark-blue
	dup 0= IF  2drop " "  THEN
	1 -rot }}text-us +image p-box .child+ blackish
    ELSE  .char  THEN ;
md-char: [ ( char -- )
    drop ]-parse 2dup "![" search nip nip IF
	drop ')' parse 2drop ]-parse + over -  THEN
    .md-text
    dup 0= IF  2drop " "  THEN
    us-state @ >r p-box >r {{ }}h box[] to p-box
    [ underline #dark-blue or ]L render-line .md-text
    p-box r> to p-box r> us-state ! blackish
    +link p-box .child+ ;
md-char: : ( char -- )
    drop /source ":" string-prefix? IF
	>in @ >r
	1 >in +! ':' parse /source ":" string-prefix? IF
	    ['] md-tokens >body find-name-in ?dup-IF
		name?int execute
		rdrop EXIT  THEN  THEN
	r> >in !
    THEN  ':' .char ;
md-char: 	 ( tab -- )
    drop dark-blue ['] wspace md-text$ $exec
    " " md-text$ 0 $ins
    {{
	{{ us-state @ md-text$ $@ }}text-us glue*l }}glue }}h box[]
    }}z box[] bx-tab >lhang
    p-box .child+ blackish  md-text$ $free ;

$10 cells buffer: indent#s
0 Value cur#indent

: indent# ( n -- ) cur#indent cells indent#s + @ ;

: >indent ( n -- )
    >in @ + source rot umin  0 -rot
    bounds U+DO  I c@ #tab = 4 and I c@ bl = 1 and or +  LOOP
    2/ dup to cur#indent
    cells >r indent#s [ $10 cells ]L r> /string
    over 1 swap +! [ 1 cells ]L /string erase ;

: bullet-char ( n -- xchar )
    "•‣‧‧‧‧‧‧‧‧‧‧‧"
    drop swap 0 ?DO xchar+ LOOP  xc@ ;
0 warnings !@

Vocabulary markdown

get-current also markdown definitions

\ headlines limited to h1..h3
: # ( -- )
    /source 2dup + 2 - 2 " #" str= -2 and +
    \huge cbl bold render-line .md-text .\\ \normal \regular ;
: ## ( -- )
    /source 2dup + 3 - 3 " ##" str= -3 and +
    \large cbl bold render-line .md-text .\\ \normal \regular ;
: ### ( -- )
    /source 2dup + 4 - 4 " ###" str= -4 and +
    \normal cbl bold render-line .md-text .\\ \normal \regular ;
: 1. ( -- )
    \ render counted line
    -3 >indent dark-blue
    {{ 0 [: cur#indent 2* 2 + spaces indent# 0 .r ." . " ;]
	$tmp }}text-us
    }}z /hfix box[] >lhang p-box .child+ blackish
    /source 0 render-line .md-text .\\ ;
synonym 2. 1.
synonym 3. 1.
synonym 4. 1.
synonym 5. 1.
synonym 6. 1.
synonym 7. 1.
synonym 8. 1.
synonym 9. 1.
: 10. ( -- )
    \ render counted line
    -4 >indent dark-blue
    {{ 0 [: cur#indent 2* 1+ spaces indent# 0 .r ." . " ;]
    $tmp }}text-us }}z /hfix box[] >lhang p-box .child+ blackish
    /source 0 render-line .md-text .\\ ;
synonym 11. 10.
synonym 12. 10.
synonym 13. 10.
synonym 14. 10.
synonym 15. 10.
synonym 16. 10.
synonym 17. 10.
synonym 18. 10.
synonym 19. 10.
synonym 20. 10.
synonym 21. 10.
synonym 22. 10.
synonym 23. 10.
synonym 24. 10.
synonym 25. 10.
synonym 26. 10.
synonym 27. 10.
synonym 28. 10.
synonym 29. 10.
synonym 30. 10.
: * ( -- )
    -2 >indent dark-blue
    {{ 0 [: cur#indent 1+ wspaces
	    cur#indent bullet-char xemit wspace ;] $tmp }}text-us
    }}z /hfix box[] >lhang p-box .child+
    blackish /source 0 render-line .md-text .\\ ;
: +  ( -- )
    -2 >indent dark-blue
    {{ 0 [: cur#indent 1+ wspaces
	'+' xemit wspace ;] $tmp }}text-us
    }}z /hfix box[] >lhang p-box .child+
    blackish /source 0 render-line .md-text .\\ ;
: -  ( -- )
    -2 >indent dark-blue
    {{ 0 [: cur#indent 1+ wspaces
	'–' xemit wspace ;] $tmp }}text-us
    }}z /hfix box[] >lhang p-box .child+
    blackish /source 0 render-line .md-text .\\ ;
: ±  ( -- )
    -2 >indent dark-blue
    {{ 0 [: cur#indent 1+ wspaces
	'±' xemit wspace ;] $tmp }}text-us
    }}z /hfix box[] >lhang p-box .child+
    blackish /source 0 render-line .md-text .\\ ;
: > ( -- )
    -2 >indent dark-blue
    {{ 0 [: cur#indent 1+ wspaces
	'|' xemit wspace ;] $tmp }}text-us
    }}z /hfix box[] >lhang p-box .child+
    blackish /source 0 render-line .md-text .\\ ;
: ::album:: ( -- )
    imgs# @ 1+ to imgs#max ;
previous set-current

warnings !

: p-format ( rw -- )
    [{: f: rw :}l rw par-split ;] v-box .do-childs ;

: ?md-token ( -- token )
    parse-name [ ' markdown >body ]L find-name-in ;
: ===/---? ( -- )
    source nip 0<> IF
	source '=' skip nip 0= IF  "# " preparse$ 0 $ins " #" preparse$ $+!
	    true  EXIT  THEN
	source '-' skip nip 0= IF  "## " preparse$ 0 $ins " ##" preparse$ $+!
	    true  EXIT  THEN
    THEN  false ;

: read-par ( -- )  0 >r
    BEGIN   r@ 1 = IF  ===/---?  IF  rdrop  refill drop  EXIT  THEN  THEN
	source dup  WHILE
	    preparse$ $@len IF  bl preparse$ c$+!  THEN
	    r@ IF  bl skip  THEN
	preparse$ $+! r> 1+ >r  source + 1- c@ '\' =  refill 0= or UNTIL
	rdrop  EXIT  THEN  rdrop 2drop ;
: read-pre ( -- )
    source 4 /string preparse$ $!  refill drop ;
: hang-source ( -- addr u hang )
    source dup >r bl skip r> over - >r
    dup >r #tab skip r> over - 2* 2* r> max ;

: hang-read ( -- )
    hang-source >r 2drop source preparse$ $+!
    BEGIN  refill  WHILE
	    hang-source r@ u>=  over 0> and  ?md-token 0=  and  WHILE
		bl preparse$ c$+!  preparse$ $+!
	REPEAT  2drop
    THEN  rdrop ;
: reset-hang ( -- )
    indent#s [ $10 cells ]L erase ;

: refill-empty ( -- flag )
    BEGIN  source nip 0=  WHILE  refill 0=  UNTIL  false  ELSE  true  THEN ;

: typeset ( -- )
    +p-box  preparse$ $@
    \ ." typesetting: '" 2dup type ''' emit cr
    [: ?md-token ?dup-IF   name?int execute
	ELSE  >in off  source 0 render-line .md-text .\\  THEN ;]
    execute-parsing  preparse$ $free ;

: pre-typeset ( -- )
    +p-box preparse$ $@ mono +emphs md-text$ $! .md-text .\\
    preparse$ $free ;

: markdown-loop ( -- )
    BEGIN  refill-empty  WHILE  reset-emph >in off
	    ?md-token  IF  hang-read typeset
	    ELSE  reset-hang
		source "    " string-prefix? IF
		    read-pre pre-typeset  ELSE
		    read-par typeset  THEN
	    THEN
    REPEAT ;

: markdown-parse ( addr u -- )
    -1 to imgs#max  imgs# off
    {{ }}v box[] to v-box nt open-fpath-file throw
    ['] markdown-loop execute-parsing-named-file
    reset-emph \regular \sans \normal ;

previous set-current

\\\
Local Variables:
forth-local-words:
    (
     (("md-char:") definition-starter (font-lock-keyword-face . 1)
      "[ \t\n]" t name (font-lock-function-name-face . 3))
    )
forth-local-indent-words:
    (
     (("md-char:") (0 . 2) (0 . 2) non-immediate)
    )
End:
