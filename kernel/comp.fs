\ compiler definitions						14sep97jaw

\ Copyright (C) 1995,1996,1997,1998,2000,2003,2004,2005,2006,2007,2008,2009,2010,2011,2012,2013,2014,2015,2016,2017,2018 Free Software Foundation, Inc.

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

\ \ Revisions-Log

\	put in seperate file				14sep97jaw	

\ \ here allot , c, A,						17dec92py

[IFUNDEF] allot
[IFUNDEF] forthstart
: allot ( n -- ) \ core
    dup unused u> -8 and throw
    dp +! ;
[THEN]
[THEN]

\ we default to this version if we have nothing else 05May99jaw
[IFUNDEF] allot
: allot ( n -- ) \ core
    \G Reserve @i{n} address units of data space without
    \G initialization. @i{n} is a signed number, passing a negative
    \G @i{n} releases memory.  In ANS Forth you can only deallocate
    \G memory from the current contiguous region in this way.  In
    \G Gforth you can deallocate anything in this way but named words.
    \G The system does not check this restriction.
    here +
    dup 1- usable-dictionary-end forthstart within -8 and throw
    dp ! ;
[THEN]

: small-allot ( n -- addr )
    dp @ tuck + dp ! ;

: c,    ( c -- ) \ core c-comma
    \G Reserve data space for one char and store @i{c} in the space.
    1 chars small-allot c! ;

: 2,	( w1 w2 -- ) \ gforth
    \G Reserve data space for two cells and store the double @i{w1
    \G w2} there, @i{w2} first (lower address).
    2 cells small-allot 2! ;

\ : aligned ( addr -- addr' ) \ core
\     [ cell 1- ] Literal + [ -1 cells ] Literal and ;

: >align ( addr a-addr -- ) \ gforth
    \G add enough spaces to reach a-addr
    swap ?DO  bl c,  LOOP ;

: align ( -- ) \ core
    \G If the data-space pointer is not aligned, reserve enough space to align it.
    here dup aligned >align ;

\ : faligned ( addr -- f-addr ) \ float f-aligned
\     [ 1 floats 1- ] Literal + [ -1 floats ] Literal and ; 

: falign ( -- ) \ float f-align
    \G If the data-space pointer is not float-aligned, reserve
    \G enough space to align it.
    here dup faligned >align ;

: maxalign ( -- ) \ gforth
    \G Align data-space pointer for all alignment requirements.
    here dup maxaligned >align ;

\ the code field is aligned if its body is maxaligned
' maxalign Alias cfalign ( -- ) \ gforth
\G Align data-space pointer for code field requirements (i.e., such
\G that the corresponding body is maxaligned).

' , alias A, ( addr -- ) \ gforth

' NOOP ALIAS const

\ \ Header							23feb93py

\ input-stream, nextname and noname are quite ugly (passing
\ information through global variables), but they are useful for dealing
\ with existing/independent defining words

: string, ( c-addr u -- ) \ gforth
    \G puts down string as cstring
    dup c, here swap chars dup allot move ;

: longstring, ( c-addr u -- ) \ gforth
    \G puts down string as longcstring
    dup , here swap chars dup allot move ;

: nlstring, ( c-addr u -- ) \ gforth
    \G puts down string as longcstring
    tuck here swap chars dup allot move , ;


[IFDEF] prelude-mask
variable next-prelude

: prelude, ( -- )
    next-prelude @ if
	align next-prelude @ ,
    then ;
[THEN]

: get-current  ( -- wid ) \ search
  \G @i{wid} is the identifier of the current compilation word list.
  current @ ;

: encode-pos ( nline nchar -- npos )
    $ff min swap 8 lshift + ;

: current-sourcepos3 ( -- nfile nline nchar )
    loadfilename# @ sourceline# input-lexeme 2@ drop source drop - ;

: encode-view ( nfile nline nchar -- xpos )
    encode-pos $7fffff min swap 23 lshift or ;

0 Value replace-sourceview \ used by #loc to modify view,

: current-sourceview ( -- xpos )
    current-sourcepos3 encode-view ;

: current-view ( -- xpos )
    replace-sourceview current-sourceview over select ;

: view, ( -- )
    current-view , 0 to replace-sourceview ;

Defer check-shadow ( addr u wid -- )
:noname drop 2drop ; is check-shadow

: header, ( c-addr u -- ) \ gforth
    name-too-long?  vt,
    get-current >r
    dup max-name-length @ max max-name-length !
    [ [IFDEF] prelude-mask ] prelude, [ [THEN] ]
    dup aligned here + dup maxaligned >align
    view,
    dup here + dup maxaligned >align
    nlstring,
    r> 1 or A, 0 A, here last !  \ link field; before revealing, it contains the
    \ tagged reveal-into wordlist
    \   alias-mask lastflags cset
    [ [IFDEF] prelude-mask ]
	next-prelude @ 0<> prelude-mask and lastflags cset
	next-prelude off
    [ [THEN] ] ;

defer record-name ( -- )
' noop is record-name
\ record next name in tags file
defer (header)
defer header ( -- ) \ gforth
' (header) IS header

: input-stream-header ( "name" -- )
    parse-name name-too-short? header, ;

: input-stream ( -- )  \ general
    \G switches back to getting the name from the input stream ;
    ['] input-stream-header IS (header) ;

' input-stream-header IS (header)

2variable nextname-string

: nextname-header ( -- )
    nextname-string 2@ header,
    nextname-string free-mem-var
    input-stream ;

\ the next name is given in the string

: nextname ( c-addr u -- ) \ gforth
    \g The next defined word will have the name @var{c-addr u}; the
    \g defining word will leave the input stream alone.
    name-too-long?
    nextname-string free-mem-var
    save-mem nextname-string 2!
    ['] nextname-header IS (header) ;

: noname, ( -- )
    0 last ! vt,  here dup cfaligned >align 0 ( alias-mask ) , 0 , 0 , ;
: noname-header ( -- )
    noname, input-stream ;

: noname ( -- ) \ gforth
    \g The next defined word will be anonymous. The defining word will
    \g leave the input stream alone. The xt of the defined word will
    \g be given by @code{latestxt}.
    ['] noname-header IS (header) ;

: latestxt ( -- xt ) \ gforth
    \G @i{xt} is the execution token of the last word defined.
    \ The main purpose of this word is to get the xt of words defined using noname
    lastcfa @ ;

' latestxt alias lastxt \ gforth-obsolete
\G old name for @code{latestxt}.

: latest ( -- nt ) \ gforth
\G @var{nt} is the name token of the last word defined; it is 0 if the
\G last word has no name.
    last @ ;

\ \ literals							17dec92py

: Literal  ( compilation n -- ; run-time -- n ) \ core
    \G Compilation semantics: compile the run-time semantics.@*
    \G Run-time Semantics: push @i{n}.@*
    \G Interpretation semantics: undefined.
    postpone lit , ; immediate restrict

: 2Literal ( compilation w1 w2 -- ; run-time  -- w1 w2 ) \ double two-literal
    \G Compile appropriate code such that, at run-time, @i{w1 w2} are
    \G placed on the stack. Interpretation semantics are undefined.
    swap postpone Literal  postpone Literal ; immediate restrict

: ALiteral ( compilation addr -- ; run-time -- addr ) \ gforth
    postpone lit A, ; immediate restrict

Defer char@ ( addr u -- char addr' u' )
:noname  over c@ -rot 1 /string ; IS char@

: char   ( '<spaces>ccc' -- c ) \ core
    \G Skip leading spaces. Parse the string @i{ccc} and return @i{c}, the
    \G display code representing the first character of @i{ccc}.
    parse-name char@ 2drop ;

: [char] ( compilation '<spaces>ccc' -- ; run-time -- c ) \ core bracket-char
    \G Compilation: skip leading spaces. Parse the string
    \G @i{ccc}. Run-time: return @i{c}, the display code
    \G representing the first character of @i{ccc}.  Interpretation
    \G semantics for this word are undefined.
    char postpone Literal ; immediate restrict

\ \ threading							17mar93py

' noop Alias recurse
\g Alias to the current definition.

unlock tlastcfa @ lock >body AConstant lastcfa
\ this is the alias pointer in the recurse header, named lastcfa.
\ changing lastcfa now changes where recurse aliases to
\ it's always an alias of the current definition
\ it won't work in a flash/rom environment, therefore for Gforth EC
\ we stick to the traditional implementation

: cfa,     ( code-address -- )  \ gforth	cfa-comma
    here
    dup lastcfa !
    0 A,
    code-address! ;

defer basic-block-end ( -- )

:noname ( -- )
    0 compile-prim1 ;
is basic-block-end

\ record locations

40 value bt-pos-width
0 AValue locs-start
$variable locs[]

: xt-location ( addr -- addr )
\ note that an xt was compiled at addr, for backtrace-locate functionality
    dup locs-start - cell/ >r
    current-sourceview dup r> 1+ locs[] $[] cell- 2! ;

has? primcentric [IF]
    has? peephole [IF]
	\ dynamic only    
	: peephole-compile, ( xt -- )
	    \ compile xt, appending its code to the current dynamic superinstruction
	    here swap , xt-location compile-prim1 ;
    [ELSE]
	: peephole-compile, ( xt -- addr ) @ , ;
    [THEN]
[ELSE]
' , is compile,
[THEN]

\ \ ticks

: default-name>comp ( nt -- w xt ) \ gforth name-to-comp
    \G @i{w xt} is the compilation token for the word @i{nt}.
    name>int ['] compile, ;

: [(')]  ( compilation "name" -- ; run-time -- nt ) \ gforth bracket-paren-tick
    (') postpone ALiteral ; immediate restrict

: [']  ( compilation. "name" -- ; run-time. -- xt ) \ core      bracket-tick
    \g @i{xt} represents @i{name}'s interpretation
    \g semantics. Perform @code{-14 throw} if the word has no
    \g interpretation semantics.
    ' postpone ALiteral ; immediate restrict

: COMP'    ( "name" -- w xt ) \ gforth  comp-tick
    \g Compilation token @i{w xt} represents @i{name}'s compilation semantics.
    parse-name forth-recognizer recognize '-error name>comp ;

: [COMP']  ( compilation "name" -- ; run-time -- w xt ) \ gforth bracket-comp-tick
    \g Compilation token @i{w xt} represents @i{name}'s compilation semantics.
    COMP' swap POSTPONE Aliteral POSTPONE ALiteral ; immediate restrict

: postpone, ( w xt -- ) \ gforth	postpone-comma
    \g Compile the compilation semantics represented by the
    \g compilation token @i{w xt}.
    dup ['] execute =
    if
	drop compile,
    else
	swap POSTPONE aliteral compile,
    then ;

include ./recognizer.fs

\ \ Strings							22feb93py

: S, ( addr u -- )
    \ allot string as counted string
    here over char+ allot  place align ;

: mem, ( addr u -- )
    \ allot the memory block HERE (do alignment yourself)
    here over allot swap move ;

: ," ( "string"<"> -- )
    [char] " parse s, ;

\ \ Header states						23feb93py

\ problematic only for big endian machines

: cset ( bmask c-addr -- )
    tuck @ or swap ! ; 

: creset ( bmask c-addr -- )
    tuck @ swap invert and swap ! ; 

: ctoggle ( bmask c-addr -- )
    tuck @ xor swap ! ; 

: lastflags ( -- c-addr )
    \ the address of the flags byte in the last header
    \ aborts if the last defined word was headerless
    latest dup 0= abort" last word was headerless"
    >f+c ;

: imm>comp  name>int ['] execute ;
: immediate ( -- ) \ core
    \G Make the compilation semantics of a word be to @code{execute}
    \G the execution semantics.
    ['] imm>comp set->comp ;

: restrict ( -- ) \ gforth
    \G A synonym for @code{compile-only}
    restrict-mask lastflags cset ;

' restrict alias compile-only ( -- ) \ gforth
\G Mark the last definition as compile-only; as a result, the text
\G interpreter and @code{'} will warn when they encounter such a word.

\ !!FIXME!! new flagless versions:
\ : immediate [: name>int ['] execute ;] set->comp ;
\ : compile-only [: drop ['] compile-only-error ;] set->int ;

\ \ Create Variable User Constant                        	17mar93py

\ : a>comp ( nt -- xt1 xt2 )  name>int ['] compile, ;

: defer@, ( xt -- )
    dup >namevt @ >vtdefer@ @ opt-something, ;

: a>int ( nt -- )  >body @ ;
: a>comp ( nt -- xt1 xt2 )  name>int ['] compile, ;
\ dup >r >body @
\    ['] execute ['] compile, r> >f+c @ immediate-mask and select ;

: s>int ( nt -- xt )  >body @ name>int ;
: s>comp ( nt -- xt1 xt2 )  >body @ name>comp ;
: s-to ( val nt -- )
    \ actually a TO: TO-OPT: word, but cross.fs does not support that
    >body @ (int-to) ;
opt: drop >body @ (comp-to) ;
: s-defer@ ( xt1 -- xt2 )
    \ actually a DEFER@ DEFER@-OPT: word, but cross.fs does not support that
    >body @ defer@ ;
opt: drop >body @ defer@, ;
: s-compile, ( xt -- )  >body @ compile, ;

: Alias    ( xt "name" -- ) \ gforth
    Header reveal ['] on vtcopy  dodefer,
    ['] a>int set->int ['] a>comp set->comp ['] s-to set-to
    ['] s-defer@ set-defer@  ['] s-compile, set-optimizer
    dup A, lastcfa ! ;

: alias? ( nt -- flag )
    >namevt @ >vt>int 2@ ['] a>comp ['] a>int d= ;

: Synonym ( "name" "oldname" -- ) \ Forth200x
    Header  ['] on vtcopy
    parse-name find-name dup 0= #-13 and throw
    dodefer, dup A,
    dup compile-only? IF  compile-only  THEN  name>int lastcfa !
    ['] s>int set->int ['] s>comp set->comp ['] s-to set-to
    ['] s-defer@ set-defer@  ['] s-compile, set-optimizer
    reveal ;

: synonym? ( nt -- flag )
    >namevt @ >vt>int 2@ ['] s>comp ['] s>int d= ;

: Create ( "name" -- ) \ core
    Header reveal dovar, ?noname-vt ;

: buffer: ( u "name" -- ) \ core ext
    Create here over 0 fill allot ;

: Variable ( "name" -- ) \ core
    Create 0 , ;

: AVariable ( "name" -- ) \ gforth
    Create 0 A, ;

: 2Variable ( "name" -- ) \ double two-variable
    Create 0 , 0 , ;

: uallot ( n -- n' ) \ gforth
    udp @ swap udp +! ;

: User ( "name" -- ) \ gforth
    Header reveal douser, ?noname-vt cell uallot , ;

: AUser ( "name" -- ) \ gforth
    User ;

: (Constant)  Header reveal docon, ?noname-vt ;

: (Value)  Header reveal dovalue, ?noname-vt ;

: Constant ( w "name" -- ) \ core
    \G Define a constant @i{name} with value @i{w}.
    \G  
    \G @i{name} execution: @i{-- w}
    (Constant) , ;

: AConstant ( addr "name" -- ) \ gforth
    (Constant) A, ;

: Value ( w "name" -- ) \ core-ext
    (Value) , ;

: AValue ( w "name" -- ) \ core-ext
    (Value) A, ;

Create !-table ' ! A, ' +! A,
Variable to-style# 0 to-style# !

: to-!, ( table -- )
    0 to-style# !@ dup 2 u< IF  cells + @ compile,  ELSE  2drop  THEN ;
: to-!exec ( table -- )
    0 to-style# !@ dup 2 u< IF  cells + perform  ELSE  2drop  THEN ;

: !!?addr!! ( -- ) to-style# @ -1 = -2056 and throw ;

: (Field)  Header reveal dofield, ?noname-vt ;

\ IS Defer What's Defers TO                            24feb93py

defer defer-default ( -- )
' abort is defer-default
\ default action for deferred words (overridden by a warning later)

: Defer ( "name" -- ) \ gforth
\G Define a deferred word @i{name}; its execution semantics can be
\G set with @code{defer!} or @code{is} (and they have to, before first
\G executing @i{name}.
    Header Reveal dodefer, ?noname-vt
    ['] defer-default A, ;

\ The following should use DEFER@: and DEFER@-OPT:, but cross.fs does
\ not support them.
: defer-defer@ ( xt -- )
    \ The defer@ implementation of children of DEFER
    >body @ ;
opt: drop ( xt -- )
    >body lit, postpone @ ;

: Defers ( compilation "name" -- ; run-time ... -- ... ) \ gforth
    \G Compiles the present contents of the deferred word @i{name}
    \G into the current definition.  I.e., this produces static
    \G binding as if @i{name} was not deferred.
    ' defer@ compile, ; immediate

\ No longer used for DOES>; integrate does>-like with ;abi-code, and
\ eliminate the extra stuff?

: does>-like ( xt -- defstart )
    \ xt ( addr -- ) is !does or !;abi-code etc, addr is the address
    \ that should be stored right after the code address.
    >r ;-hook ?struc
    exit-like
    here [ has? peephole [IF] ] 5 [ [ELSE] ] 4 [ [THEN] ] cells +
    postpone aliteral r> compile, [compile] exit
    [ has? peephole [IF] ] finish-code [ [THEN] ]
    defstart ;

\ call with locals - unused

\ docolloc-dummy (docolloc-dummy)

\ opt: to define compile, action

Create vttemplate
0 A,                   \ link field
' peephole-compile, A, \ compile, field
' no-to A,             \ to field
' default-name>int A,  \ name>int field
' default-name>comp A, \ name>comp field
' no-defer@ A,         \ defer@
0 A,                   \ extra field

\ initialize to one known vt

: (make-latest) ( xt1 xt2 -- )
    swap >namevt @ vttemplate vtsize move
    >namevt vttemplate over ! vttemplate ! ;
: vtcopy ( xt -- ) \ gforth vtcopy
    here (make-latest) ;

: vtcopy,     ( xt -- )  \ gforth	vtcopy-comma
    dup vtcopy here >r dup >code-address cfa, cell+ @ r> cell+ ! ;

: vtsave ( -- addr u ) \ gforth
    \g save vttemplate for nested definitions
    vttemplate vtsize save-mem  vttemplate off ;

: vtrestore ( addr u -- ) \ gforth
    \g restore vttemplate
    over >r vttemplate swap move r> free throw ;

: vt= ( vt1 vt2 -- flag )
    cell+ swap vtsize cell /string tuck compare 0= ;

: (vt,) ( -- )
    align  here vtsize allot vttemplate over vtsize move
    vtable-list @ over !  dup vtable-list !
    vttemplate @ !  vttemplate off ;

: vt, ( -- )
    vttemplate @ 0= IF EXIT THEN
    vtable-list
    BEGIN  @ dup  WHILE
	    dup vttemplate vt= IF  vttemplate @ !  vttemplate off  EXIT  THEN
    REPEAT  drop (vt,) ;

: make-latest ( xt -- )
    vt, dup last ! dup lastcfa ! dup (make-latest) ;

: !namevt ( addr -- )  latestxt >namevt ! ;

: start-xt ( -- colonsys xt ) \ incomplete, will not be a full xt
    here >r docol: cfa, colon-sys ] :-hook r> ;
: start-xt-like ( colonsys xt -- colonsys )
    reveal does>-like drop start-xt drop ;

: set-optimizer ( xt -- ) vttemplate >vtcompile, ! ;
' set-optimizer alias set-compiler
: set-to        ( to-xt -- ) vttemplate >vtto ! ;
: set-defer@    ( defer@-xt -- ) vttemplate >vtdefer@ ! ;
: set->int      ( xt -- ) vttemplate >vt>int ! ;
: set->comp     ( xt -- ) vttemplate >vt>comp ! ;
: set-does>     ( xt -- ) vttemplate >vtextra !
    created?  IF  ['] does, set-optimizer  THEN
    dodoes: latestxt ! ;

:noname ( -- colon-sys ) start-xt  set-optimizer ;
:noname ['] set-optimizer start-xt-like ;
over over
interpret/compile: opt:
interpret/compile: comp:
( compilation colon-sys1 -- colon-sys2 ; run-time nest-sys -- ) \ gforth

: default-to-opt ( xt1 xt2 -- )
    swap lit, :, ;
: to: ( "name1" -- colon-sys ) \ gforth-internal
    \G Defines a to-word ( v xt -- ) that is not a proper word (it does
    \G not compile properly), but only useful as parameter for
    \G @code{set-to}.  The to-word constitutes a part of the TO <name>
    \G run-time semantics: it stores v (a stack item of the appropriate
    \G type for <name>) in the storage represented by the xt (which is
    \G the xt of <name>).  It is usually used only for interpretive
    \G @code{to}; the compiled @code{to} uses the part after
    \G @code{to-opt:}.
    : ['] default-to-opt set-optimizer ;
: to-opt: ( -- colon-sys ) \ gforth-internal
    \G Must only be used to modify a preceding to-word defined with
    \G \code{to:}.  It defines a part of the TO <name> run-time
    \G semantics used with compiled \code{TO}.  The stack effect of the
    \G code following @code{to-opt:} must be: ( xt -- ) ( generated: v
    \G -- ).  The generated code stores v in the storage represented by
    \G xt.
    start-xt  set-optimizer postpone drop ;

\ defer and friends

' to: alias defer@:  ( "name1" -- colon-sys ) \ gforth-internal
\g Defines @i{name1}, not a proper word, only useful as parameter for
\g @code{set-defer@}.  It defines what @code{defer@} does for the word
\g to which the @code{set-defer@} is applied.  If there is a
\g @code{defer@-opt:} following it, that provides optimized code
\g generation for compiled @code{action-of}.
' to-opt: alias defer@-opt: ( -- colon-sys ) \ gforth-internal
\g Optimized code generation for compiled @code{action-of @i{name}}.
\g The stack effect of the following code must be ( xt -- ), where xt
\g represents @i{name}; this word generates code with stack effect (
\g -- xt1 ), where xt1 is the result of xt @code{defer@}.

' (int-to) alias defer! ( xt xt-deferred -- ) \ gforth  defer-store
\G Changes the @code{defer}red word @var{xt-deferred} to execute @var{xt}.

: (comp-to) ( xt -- ) ( generated code: v -- )
    \g in compiled @code{to @i{name}}, xt is that of @i{name}.  This
    \g word generates code for storing v (of type appropriate for
    \g @i{name}) there.  This word is a factor of @code{to}.
    dup >namevt @ >vtto @ opt-something, \ this OPT-SOMETHING, calls the
    \ TO-OPT: part of the SET-TO part of the defining word of <name>.
;

\ The following should use TO: OPT-TO:, but that's not supported by cross.fs
: value-to ( n value-xt -- ) \ gforth-internal
    \g this is the TO-method for normal values; it's tickable, but the
    \g only purpose of its xt is to be consumed by @code{set-to}.  It
    \g does not compile like a proper word.
    >body !-table to-!exec ;
opt: drop ( value-xt -- ) \ run-time: ( n -- )
     >body postpone ALiteral !-table to-!, ;

: <IS> ( "name" xt -- ) \ gforth
    \g Changes the @code{defer}red word @var{name} to execute @var{xt}.
    record-name (') (int-to) ;

: [IS] ( compilation "name" -- ; run-time xt -- ) \ gforth bracket-is
    \g At run-time, changes the @code{defer}red word @var{name} to
    \g execute @var{xt}.
    record-name (') (comp-to) ; immediate restrict

' <IS> ' [IS] interpret/compile: TO ( value "name" -- ) \ core-ext
\g changes the value of @var{name} to @var{value}
' <IS> ' [IS] interpret/compile: IS ( value "name" -- ) \ core-ext
\g changes the @code{defer}red word @var{name} to execute @var{value}

: <+TO>  1 to-style# ! <IS> ;
: <addr>  -1 to-style# ! <IS> ;

: [+TO]  1 to-style# ! postpone [IS] ; immediate restrict
: [addr]  -1 to-style# ! postpone [IS] ; immediate restrict

' <+TO> ' [+TO] interpret/compile: +TO ( value "name" -- ) \ gforth
\g increments the value of @var{name} by @var{value}
' <addr> ' [addr] interpret/compile: addr ( "name" -- addr ) \ gforth
\g provides the address @var{addr} of the value stored in @var{name}

\ \ : ;                                                  	24feb93py

defer :-hook ( sys1 -- sys2 )
defer free-old-local-names ( -- )
defer ;-hook ( sys2 -- sys1 )
defer 0-adjust-locals-size ( -- )

1 value colon-sys-xt-offset
\g you get the xt in a colon-sys with COLON-SYS-XT-OFFSET PICK

0 Constant defstart
: colon-sys ( -- colon-sys )
    \ a colon-sys consists of an xt for an action to be executed at
    \ the end of the definition, possibly some data consumed by the xt
    \ below that, and a DEFSTART tag on top; the stack effect of xt is
    \ ( ... -- ), where the ... is the additional data in the
    \ colon-sys.  The :-hook may add more stuff (which is then removed
    \ by ;-hook before this stuff here is processed).
    ['] noop defstart ;

: (noname->comp) ( nt -- nt xt )  ['] compile, ;
: (:noname) ( -- colon-sys )
    \ common factor of : and :noname
    docol, colon-sys ] :-hook ( unlocal-state off ) ;

: : ( "name" -- colon-sys ) \ core	colon
    free-old-local-names
    Header (:noname) ?noname-vt ;

: noname-vt ( -- )
    \G modify vt for noname words
    ['] noop set->int  ['] (noname->comp) set->comp ;
: ?noname-vt ( -- ) last @ 0= IF  noname-vt  THEN ;

: :noname ( -- xt colon-sys ) \ core-ext	colon-no-name
    noname, here (:noname) noname-vt ;

: ; ( compilation colon-sys -- ; run-time nest-sys ) \ core	semicolon
    ;-hook [compile] exit ?colon-sys
    [ has? peephole [IF] ] finish-code [ [THEN] ]
    reveal postpone [ ; immediate restrict

: concat ( xt1 xt2 -- xt )
    \ concat two xts into one
    >r >r :noname r> compile, r> compile, postpone ; ;

: rectype ( int-xt comp-xt post-xt -- rectype )
    \G create a new unnamed recognizer token
    here >r rot , swap , , r> ;

: rectype: ( int-xt comp-xt post-xt "name" -- )
    \G create a new recognizer table
    Create rectype drop ;

\ does>

: created? ( -- flag )
    vttemplate >vtcompile, @ ['] variable, = ;

: comp-does>; ( some-sys flag lastxt -- )
    \ used as colon-sys xt; this is executed after ";" has removed the
    \ colon-sys produced by [:
    nip (;]) postpone set-does> postpone ; ;

: comp-does> ( compilation colon-sys1 -- colon-sys2 )
    state @ >r
    comp-[:
    r> 0= if postpone [ then \ don't change state
    ['] comp-does>; colon-sys-xt-offset stick \ replace noop with comp-does>;
; immediate

: int-does>; ( flag lastxt -- )
    nip >r vt, wrap! r> set-does> ;

: int-does> ( -- colon-sys )
    int-[:
    ['] int-does>; colon-sys-xt-offset stick \ replace noop with :does>;
;

' int-does> ' comp-does> interpret/compile: does> ( compilation colon-sys1 -- colon-sys2 )

\ for cross-compiler's interpret/compile:

: i/c>comp ( nt -- xt1 xt2 )
    >body cell+ @ ['] execute ;

\ \ Search list handling: reveal words, recursive		23feb93py

: last?   ( -- false / nfa nfa )
    latest ?dup ;

: (nocheck-reveal) ( nt wid -- )
    wordlist-id dup >r
    @ over >link ! 
    r> ! ;
: (reveal) ( nt wid -- )
    over name>string 2 pick check-shadow
    (nocheck-reveal) ;

\ make entry in wordlist-map
' (reveal) f83search reveal-method !

: reveal ( -- ) \ gforth
    last?
    if \ the last word has a header
	dup >link @ 1 and
	if \ it is still hidden
	    dup >link @ 1 xor		( nt wid )
	    dup wordlist-map @ reveal-method perform
	else
	    drop
	then
    then ;

: rehash  ( wid -- )
    dup wordlist-map @ rehash-method perform ;

' reveal alias recursive ( compilation -- ; run-time -- ) \ gforth
\g Make the current definition visible, enabling it to call itself
\g recursively.
	immediate restrict
