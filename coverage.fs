\ Code coverage tool

\ Copyright (C) 2018 Free Software Foundation, Inc.

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

require sections.fs

unused extra-section coverage

' Create coverage cover-start

: cover-end ( -- addr ) ['] here coverage ;
: cover, ( n -- ) ['] , coverage ;
: cover-end! ( addr -- )  [: dp ! ;] coverage ;

[IFUNDEF] coverage?
    0 Value coverage? ( -- flag ) \ gforth-exp
    \G Coverage check on/off
[THEN]
0 value dead-cov?

: cov+, ( -- )
    coverage?  dead-code @ 0= and  loadfilename# @ 0>= and  IF
	current-sourceview input-lexeme @ + cover,
	postpone inc# cover-end , 0 cover,
    THEN
    false to dead-cov? ;

: cov+ ( -- ) \ gforth-exp
    \G add a coverage tag here
    dead-cov? 0= state @ and  IF  cov+,  THEN
    false to dead-cov? ; immediate compile-only
: ?cov+ ( flag -- flag ) \ gforth-exp
    ]] dup IF ELSE THEN [[ ; immediate compile-only

:noname defers :-hook                     cov+, ; is :-hook
:noname defers if-like            postpone cov+ ; is if-like
:noname defers until-like         postpone cov+ ; is until-like
:noname defers basic-block-end    postpone cov+ ; is basic-block-end
:noname defers exit-like      true to dead-cov? ; is exit-like
:noname defers before-line        postpone cov+ ; is before-line

: cov% ( -- ) \ gforth-exp
    \G print the coverage percentage
    0 cover-end cover-start U+DO
	I cell+ @ 0<> -
    2 cells +LOOP  #2000 cells cover-end cover-start - */
    0 <# '%' hold # '.' hold #s #> type ."  coverage" ;

: .cover-raw ( -- ) \ gforth-exp
    \G print all raw coverage data
    cover-end cover-start U+DO
	I @ .sourceview ." : " I cell+ ? cr
    2 cells +LOOP ;

Defer .cov#

: .ansi-cov# ( n -- )
    >r info-color error-color r@ select
    dup Invers or attr! space r> 0 .r  attr! ;
: .paren-cov# ( n -- ) ."  ( " 0 .r ." ) " ;

: color-cover ( -- ) ['] .ansi-cov#  is .cov# ;
\G print coverage with colors
: bw-cover    ( -- ) ['] .paren-cov# is .cov# ;
\G print coverage with parents (source-code compatible)
color-cover

: ?del-cover ( addr u -- n )
    \G remove coverage comment
    2dup s"  ( " string-prefix?  IF
	3 dup >r /string
	BEGIN  over c@ digit?  WHILE  drop 1 /string r> 1+ >r  REPEAT
	s" ) " string-prefix? IF  r> 2 +  ELSE  rdrop  0  THEN
    ELSE  2drop  0  THEN ;

: .cover-file { fn -- } \ gforth-exp
    \G pretty print coverage in a file
    fn included-buffer 0 locate-line 0 { d: buf lpos d: line cpos }
    cover-end cover-start U+DO
	I @ view>filename# fn = IF
	    buf lpos
	    BEGIN  dup I @ view>line u<  WHILE
		    line cpos safe/string type cr default-color attr!
		    locate-line  to line  0 to cpos
	    REPEAT  to lpos  to buf
	    line cpos safe/string
	    over I @ view>char cpos - tuck type +to cpos  2drop
	    I cell+ @ .cov#
	    line cpos safe/string ?del-cover +to cpos
	THEN
    2 cells +LOOP
    line cpos safe/string type cr  default-color attr!  buf type ;

: covered? ( fn -- flag ) \ gforth-exp
    \G check if file number @var{fn} has coverage information
    false cover-end cover-start U+DO 
	over I @ view>filename# = or
    2 cells +LOOP  nip ;

: .coverage ( -- ) \ gforth-exp
    \G pretty print coverage
    cr included-files $[]# 0 ?DO
	I covered? IF
	    I [: included-files $[]@ type ':' emit cr ;]
	    warning-color color-execute
	    I .cover-file
	THEN
    LOOP ;

: annotate-cov ( -- ) \ gforth-exp
    \G annotate files with coverage information
    included-files $[]# 0 ?DO
	I covered? IF
	    I [: included-files $[]@ type ." .cov" ;] $tmp
	    r/w create-file dup 0= IF
		drop { fd }
		I ['] .cover-file fd outfile-execute  fd close-file throw
	    ELSE
		I [: included-files $[]@ type space
		    .error-string cr ;] warning-color color-execute
		drop  THEN \ ignore write errors
	THEN
    LOOP ;

\ load and save coverage

$10 buffer: cover-hash

: hash-cover ( -- addr u ) \ gforth-exp
    cover-hash $10 erase
    cover-end cover-start U+DO
	I cell false cover-hash hashkey2
    2 cells +LOOP
    cover-hash $10 ;

: cover-filename ( -- addr u ) \ gforth-exp
    "~/.cache/gforth/" 2dup $1ff mkdir-parents drop
    [: type
	hash-cover bounds ?DO  I c@ 0 <# # # #> type LOOP ." .covbin" ;]
    ['] $tmp $10 base-execute ;

: save-cov ( -- ) \ gforth-exp
    \G save coverage counters
    cover-filename r/w create-file throw >r
    cover-start cover-end over - r@ write-file throw
    r> close-file throw ;

: load-cov ( -- ) \ gforth-exp
    \G load coverage counters
    cover-filename r/o open-file dup #-514 = IF
	2drop true [: ." no saved coverage found" cr ;] ?warning
	EXIT  THEN  throw  >r
    cover-start r@ file-size throw drop r@ read-file throw
    cover-start + cover-end!
    r> close-file throw ;

true to coverage?

\ coverage tests

[defined] test-it [IF]
    : test1 ( n -- )  0 ?DO  I 3 > ?LEAVE I . LOOP ;
    : yes ." yes" ;
    : no  ." no" ;
    : test2 ( flag -- ) IF  yes  ELSE  no  THEN ;
[THEN]
