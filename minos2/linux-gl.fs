\ Linux bindings for GLES

\ Copyright (C) 2014,2016,2017,2018 Free Software Foundation, Inc.

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

require unix/x.fs
require mini-oof2.fs
require struct-val.fs

also x11

0 Value dpy
0 Value screen-struct
0 Value screen
0 Value win
0 Value ic
0 Value im
0 Value xim
0 Value fontset

&31 Constant XA_STRING8
1 Constant XA_PRIMARY

&31 Value XA_STRING
4 Value XA_TARGETS
4 Value XA_COMPOUND_TEXT
1 Value XA_CLIPBOARD
0 Value _NET_WM_BYPASS_COMPOSITOR
0 Value _NET_WM_STATE
0 Value _NET_WM_STATE_FULLSCREEN

require need-x.fs

Variable kbflag

XIMPreeditPosition XIMPreeditArea or
XIMPreeditNothing or XIMPreeditNone or Constant XIMPreedit

: best-im ( -- im )
    XSupportsLocale IF
	"XMODIFIERS" getenv dup IF
	    XSetLocaleModifiers 0= IF
		." Warning: Cannot set locale modifiers to '"
		"XMODIFIERS" getenv type  ." '" cr THEN
	ELSE  2drop  THEN
    THEN
    dpy 0 0 0 XOpenIM dup to xim
    ?dup-0=-IF  "@im=local" XSetLocaleModifiers drop
	dpy 0 0 0 XOpenIM dup to xim
	." Warning: can't open XMODIFIERS' IM, set to '@im=local' instead" cr
    THEN
    IF  0 { w^ styles } xim "queryInputStyle\0" drop styles XGetIMValues
	0<> ?EXIT \ didn't succeed
	0  styles @ cell+ @ styles @ w@ cells bounds ?DO
	    I @ dup XIMPreedit and 0<> swap XIMStatusNothing and 0<> and
	    IF  drop I @  LEAVE  THEN
	cell +LOOP  dup 0= IF ." No style found" cr  THEN
	styles @ XFree drop
    ELSE  0  THEN ;

: set-fontset ( -- )
    dpy "-*-FreeSans-*-r-*-*-*-120-*-*-*-*-*-*,-misc-fixed-*-r-*-*-*-130-*-*-*-*-*-*" 0 0 0 { w^ misslist w^ miss# w^ defstring }
    misslist miss# defstring XCreateFontSet to fontset
    misslist @ XFreeStringList ;

: get-display ( -- w h )
    "DISPLAY" getenv XOpenDisplay to dpy
    dpy 0= abort" Can't open display!"
    dpy XDefaultScreenOfDisplay to screen-struct
    dpy XDefaultScreen to screen
    best-im to im  set-fontset
    dpy #38 0 XKeycodeToKeysym drop
    screen-struct Screen-width l@
    screen-struct Screen-height l@ ;

4 buffer: spot \ spot location, two shorts

: get-ic ( win -- ) xim 0= IF  drop  EXIT  THEN
    ic IF  >r ic "focusWindow\0" drop r> XSetICValues drop
	EXIT  THEN
    0 "fontSet\0" drop fontset "spotLocation\0" drop spot
    XVaCreateNestedList_2 { win list }
    xim "inputStyle\0" drop im "preeditAttributes\0" drop list
    "focusWindow\0" drop win XCreateIC_3 dup to ic
    list XFree drop
    ?dup-IF  XSetICFocus  THEN ;

: focus-ic ( win -- )  ic IF
	>r ic "focusWindow\0" drop r@ "clientWindow\0" drop r>
	XSetICValues_2 drop  ic XSetICFocus
    THEN ;

: get-atoms ( -- )
    dpy "CLIPBOARD" 0 XInternAtom to XA_CLIPBOARD
    dpy "TARGETS" 0 XInternAtom to XA_TARGETS
    dpy "COMPOUND_TEXT" 0 XInternAtom to XA_COMPOUND_TEXT
    dpy "_NET_WM_BYPASS_COMPOSITOR" 0 XInternAtom to _NET_WM_BYPASS_COMPOSITOR
    dpy "_NET_WM_STATE" 0 XInternAtom to _NET_WM_STATE
    dpy "_NET_WM_STATE_FULLSCREEN" 0 XInternAtom to _NET_WM_STATE_FULLSCREEN
    max-single-byte $80 = IF
	dpy "UTF8_STRING" 0 XInternAtom  ELSE  XA_STRING8  THEN
    to XA_STRING ;

0
KeyPressMask or
KeyReleaseMask or
ButtonPressMask or
ButtonReleaseMask or
EnterWindowMask or
LeaveWindowMask or
PointerMotionMask or
ButtonMotionMask or
\ KeymapStateMask or
ExposureMask or
\ VisibilityChangeMask or
StructureNotifyMask or
\ ResizeRedirectMask or
\ SubstructureNotifyMask or
\ SubstructureRedirectMask or
FocusChangeMask or
PropertyChangeMask or
\ ColormapChangeMask or
\ OwnerGrabButtonMask or
Constant default-events

[IFUNDEF] linux  : linux ;  [THEN]

Defer window-init     ' noop is window-init
Defer config-changed
Defer screen-ops      ' noop is screen-ops
Defer reload-textures ' noop is reload-textures

: getwh ( -- )  0 0 dpy-w @ dpy-h @ glViewport ;

:noname ( -- ) +sync +config ( getwh ) ; is config-changed

: term-cr defers cr ;

begin-structure app_input_state
field: action
field: flags
field: metastate
field: edgeflags
field: pressure
field: size
2field: downtime
2field: eventtime
2field: eventtime'
field: tcount
field: x0
field: y0
field: x1
field: y1
field: x2
field: y2
field: x3
field: y3
field: x4
field: y4
field: x5
field: y5
field: x6
field: y6
field: x7
field: y7
field: x8
field: y8
field: x9
field: y9
end-structure

app_input_state buffer: *input

Variable rendering -2 rendering !
Variable ?sync-update
4 buffer: wm_delete_window
4 buffer: wm_sync_request
4 buffer: wm_sync_counter
8 buffer: wm_sync_value
8 buffer: wm_sync_value'

: xsv! ( ud addr -- )
    >r 2dup #32 drshift drop r@ l! drop r> 4 + l! ;
: xsv@ ( addr -- ud )
    >r r@ 4 + l@ 0 r> sl@ s>d #32 dlshift d+ ;

: sync-counter-update ( -- )
    dpy wm_sync_counter l@ wm_sync_value' XSyncSetCounter drop ;

[IFUNDEF] level#
    Variable level#
[THEN]

\ handle X11 events

object class
    drop 0 XGenericEvent-type        lvalue: e.type
    drop 0 XMotionEvent-time         value: e.kbm.time \ key, button, motion, crossing
    drop 0 XPropertyEvent-time       value: e.psc.time
    drop 0 XSelectionRequestEvent-time  value: e.sr.time
    drop 0 XSelectionEvent-time      value: e.s.time
    drop 0 XGenericEvent-serial      value: e.serial
    drop 0 XGenericEvent-send_event  lvalue: e.send_event
    drop 0 XGenericEvent-display     value: e.display
    drop 0 XAnyEvent-window          value: e.window
    drop 0 XResizeRequestEvent-width     lvalue: e.r-width
    drop 0 XResizeRequestEvent-height    lvalue: e.r-height
    drop 0 XConfigureRequestEvent-width  lvalue: e.c-width
    drop 0 XConfigureRequestEvent-height lvalue: e.c-height
    drop 0 XCreateWindowEvent-width      lvalue: e.w-width
    drop 0 XCreateWindowEvent-height     lvalue: e.w-height
    drop 0 XExposeEvent-width            lvalue: e.e-width
    drop 0 XExposeEvent-height           lvalue: e.e-height
    drop 0 XButtonEvent-x            slvalue: e.x
    drop 0 XButtonEvent-y            slvalue: e.y
    drop 0 XButtonEvent-button       lvalue: e.button
    drop 0 XKeyEvent-state           lvalue: e.state
    drop 0 XKeyEvent-keycode         lvalue: e.code \ key and button
    drop 0 XSelectionRequestEvent-selection value: e.selection
    drop 0 XSelectionRequestEvent-target    value: e.target
    drop 0 XSelectionRequestEvent-requestor value: e.requestor
    drop 0 XSelectionRequestEvent-property  value: e.property
    drop 0 XSelectionEvent-requestor value: e.requestor'
    drop 0 XSelectionEvent-property  value: e.property'
    drop 0 XSelectionEvent-target    value: e.target'
    drop 0 XClientMessageEvent-data  value: e.data
    drop 0 XEvent var event
    XEvent var xev \ for sending events
    $100 var look_chars
    4 var look_key
    4 var comp_stat
    method DoNull \ doesn't exist
    method DoOne  \ doesn't exist, either
    method DoKeyPress
    method DoKeyRelease
    method DoButtonPress
    method DoButtonRelease
    method DoMotionNotify
    method DoEnterNotify
    method DoLeaveNotify
    method DoFocusIn
    method DoFocusOut
    method DoKeymapNotify
    method DoExpose
    method DoGraphicsExpose
    method DoNoExpose
    method DoVisibilityNotify
    method DoCreateNotify
    method DoDestroyNotify
    method DoUnmapNotify
    method DoMapNotify
    method DoMapRequest
    method DoReparentNotify
    method DoConfigureNotify
    method DoConfigureRequest
    method DoGravityNotify
    method DoResizeRequest
    method DoCirculateNotify
    method DoCirculateRequest
    method DoPropertyNotify
    method DoSelectionClear
    method DoSelectionRequest
    method DoSelectionNotify
    method DoColormapNotify
    method DoClientMessage
    method DoMappingNotify
    method DoGenericEvent
end-class handler-class

User event-handler  handler-class new event-handler !

\ selection

Variable own-selection
Variable got-selection
$10000 Constant /propfetch
$100 /propfetch * Constant propfetchs

: post-selection ( addr u selection-number -- )
    -rot 2dup dpy -rot XStoreBytes drop
    event-handler @ >o
    >r >r
    dpy dpy XDefaultRootWindow 9 \ cut buffer 0
    XA_STRING 8 PropModeReplace
    r> r> XChangeProperty drop
    dpy swap win CurrentTime XSetSelectionOwner drop
    own-selection on o> ;
: fetch-property ( win prop target addr -- )
    0 0 0 0 0 { target addr w^ ret-t w^ form-t w^ n w^ rest w^ prop }
    addr $free
    propfetchs 0 ?DO
	dpy -rot I /propfetch 1 AnyPropertyType
	ret-t form-t n rest prop XGetWindowProperty 0= IF
	    prop @ n @ addr $+! 
	THEN
	rest @ 0= ?LEAVE
    /propfetch +LOOP
    got-selection on ;

Variable primary$

: clipboard! ( addr u -- )
    2dup xpaste! XA_CLIPBOARD post-selection ;
: primary! ( addr u -- )
    2dup primary$ $! XA_PRIMARY post-selection ;

\ keys

Variable exposed

: $, ( addr u -- )  here over 1+ allot place ;

also x11

: xmeta@ ( state -- meta ) >r
    r@ ShiftMask   and 0<> 1 and
    r@ Mod1Mask    and 0<> 2 and or
    r> ControlMask and 0<> 4 and or ;

: +meta ( addr u -- addr' u' ) \ insert meta information
    >r over c@ #esc <> IF  rdrop  EXIT  THEN
    r> dup 0= IF  drop  EXIT  THEN  '1' + \ no meta, don't insert
    [: >r 1- 2dup + c@ >r
	over 1+ c@ '[' = IF
	    2dup 1- + c@ '9' 1+ '0' within
	    IF  type ." 1;"  ELSE  type ." ;"  THEN
	ELSE  type  THEN
    r> r> emit emit ;] $tmp ;

Create x-key>ekey \ very minimal set for a start
XK_BackSpace , "\x7F" $,
XK_Tab       , "\t" $,
XK_Linefeed  , "\n" $,
XK_Return    , "\r" $,
XK_Home      , "\e[H" $,
XK_Left      , "\e[D" $,
XK_Up        , "\e[A" $,
XK_Right     , "\e[C" $,
XK_Down      , "\e[B" $,
XK_Insert    , "\e[2~" $,
XK_Delete    , "\e[3~" $,
XK_Prior     , "\e[5~" $,
XK_Next      , "\e[6~" $,
XK_F1        , "\eOP" $,
XK_F2        , "\eOQ" $,
XK_F3        , "\eOR" $,
XK_F4        , "\eOS" $,
XK_F5        , "\e[15~" $,
XK_F6        , "\e[17~" $,
XK_F7        , "\e[18~" $,
XK_F8        , "\e[19~" $,
XK_F9        , "\e[20~" $,
XK_F10       , "\e[21~" $,
XK_F12       , "\e[22~" $,
XK_F12       , "\e[23~" $,
0 , 0 c,
DOES> ( x-key -- addr u )
  swap >r
  BEGIN  dup cell+ swap @ dup r@ <> and WHILE  count +  REPEAT
  count rdrop e.state xmeta@ +meta ;

previous

0 Value timeoffset
: XTime0 ( -- ntime ) utime #1000 ud/mod drop nip ;
: XTime ( -- ntime ) XTime0 timeoffset + ;

: screen-orientation ( -- 0/1 )
    dpy-w @ dpy-h @ > negate ;
: .atom ( n -- )
    ?dup-IF  dpy swap XGetAtomName cstring>sstring type
    ELSE  ." atom (null)"  THEN ;

' noop handler-class to DoNull \ doesn't exist
' noop handler-class to DoOne  \ doesn't exit, either
:noname  ic event look_chars $FF look_key comp_stat  Xutf8LookupString
    dup 1 = IF  look_chars c@ dup $7F = swap 8 = or +  THEN \ we want the other delete
    ?dup-IF  look_chars swap
    ELSE   look_key l@ x-key>ekey  THEN
    2dup "\e" str= level# @ 0> and IF  2drop -1 level# +!  ELSE  inskeys  THEN
; handler-class to DoKeyPress
' noop handler-class to DoKeyRelease
:noname  0 *input action ! 1 *input pressure !
    *input eventtime 2@ *input eventtime' 2!
    e.kbm.time s>d *input eventtime 2!  #0. *input downtime 2!
    e.kbm.time XTime0 - to timeoffset
    e.x e.y *input y0 ! *input x0 ! ; handler-class to DoButtonPress
:noname  1 *input action ! 0 *input pressure !
    *input eventtime 2@ *input eventtime' 2!
    e.kbm.time s>d 2dup *input eventtime 2@ d- *input downtime 2!
    e.kbm.time XTime0 - to timeoffset
    *input eventtime 2!
    e.x *input x0 ! e.y *input y0 ! ; handler-class to DoButtonRelease
:noname
    *input pressure @ IF
	2 *input action !
	e.kbm.time s>d *input eventtime 2@ d- *input downtime 2!
	e.kbm.time XTime0 - to timeoffset
	e.x e.y *input y0 ! *input x0 !
    THEN ; handler-class to DoMotionNotify
' noop handler-class to DoEnterNotify
' noop handler-class to DoLeaveNotify
:noname e.window focus-ic ; handler-class to DoFocusIn
' noop handler-class to DoFocusOut
' noop handler-class to DoKeymapNotify
:noname exposed on ; handler-class to DoExpose
:noname exposed on ; handler-class to DoGraphicsExpose
' noop handler-class to DoNoExpose
' noop handler-class to DoVisibilityNotify
' noop handler-class to DoCreateNotify
' noop handler-class to DoDestroyNotify
' noop handler-class to DoUnmapNotify
' noop handler-class to DoMapNotify
' noop handler-class to DoMapRequest
' noop handler-class to DoReparentNotify
:noname  e.c-height e.c-width dpy-w ! dpy-h !
    ctx IF  config-changed  ELSE  getwh  THEN ; handler-class to DoConfigureNotify
' noop handler-class to DoConfigureRequest
' noop handler-class to DoGravityNotify
:noname  e.r-width dpy-w ! e.r-height dpy-h ! config-changed ; handler-class to DoResizeRequest
' noop handler-class to DoCirculateNotify
' noop handler-class to DoCirculateRequest
:noname e.psc.time XTime0 - to timeoffset ; handler-class to DoPropertyNotify
:noname  e.psc.time XTime0 - to timeoffset
    own-selection off ; handler-class to DoSelectionClear

: rest-request { addr n mode format type -- }
    dpy e.requestor e.property
    type format mode addr n
    XChangeProperty drop dpy 0 XSync drop ;
: paste@ ( -- addr u )
    case  e.selection
	XA_PRIMARY   of  primary$ $@  endof
	XA_CLIPBOARD of  paste$   $@  endof
	s" " rot
    endcase ;
: string-request ( -- )
    paste@ PropModeReplace 8 XA_STRING rest-request ;
\ : string8-request ( -- )
\     paste@ PropModeReplace 8 XA_STRING8 rest-request ;
: compound-request ( -- )  string-request ;
4 buffer: 'string
: target-request ( -- )
    XA_STRING 'string l!
    'string 1 PropModeReplace #32 4 rest-request ;
: do-request ( atom -- )
    \ dup .atom cr
    case
\	XA_STRING8        of  string8-request   endof
	XA_STRING         of  string-request    endof
	XA_TARGETS        of  target-request    endof
	XA_COMPOUND_TEXT  of  compound-request  endof
	." Unknown request: "
	dpy over XGetAtomName cstring>sstring type cr
    endcase ;
: selection-request ( -- )
\    ." Selection Request from: " e.requestor hex.
\    e.selection .atom space
\    e.property .atom cr
    e.sr.time XTime0 - to timeoffset
    event xev 0 XSelectionEvent-requestor move \ first copy the event
    event XSelectionRequestEvent-requestor xev XSelectionEvent-requestor
    [ XSelectionEvent negate XSelectionEvent-requestor negate ]L move
    e.property xev XSelectionEvent-property !
    1 xev XSelectionEvent-send_event l!
    SelectionNotify xev XSelectionEvent-type l!
    e.target do-request
    dpy e.requestor 0 0 xev XSendEvent drop ;
' selection-request handler-class to DoSelectionRequest
:noname ( -- )
    e.s.time XTime0 - to timeoffset
    e.requestor' e.property'
    \ dup .atom  e.target' .atom
    case dup
	XA_PRIMARY    of  primary$  endof
	XA_CLIPBOARD  of  paste$    endof
	drop 2drop  got-selection on ( we got nothing ) EXIT
    endcase  e.target' swap fetch-property
; handler-class to DoSelectionNotify
' noop handler-class to DoColormapNotify
:noname ( -- )  e.data
    wm_delete_window l@ =  IF  -1 level# +!  THEN
; handler-class to DoClientMessage
' noop handler-class to DoMappingNotify
' noop handler-class to DoGenericEvent

: handle-event ( -- ) e.type cells o#+ [ -1 cells , ] @ + perform ;
#16 Value looper-to# \ 16ms, don't sleep too long
: get-events ( -- )
    looper-to# #4000000 um* ntime d+ { d: timeout }
    event-handler @ >o
    BEGIN  dpy XPending  ntime timeout du< and
    WHILE  dpy event XNextEvent drop
	    event 0 XFilterEvent 0= IF  handle-event  THEN
    REPEAT o> ;

\ polling of FDs

get-current also forth definitions

require unix/socket.fs
require unix/pthread.fs

previous set-current

User xptimeout  cell uallot drop
looper-to# #1000000 um* xptimeout 2!
3 Value xpollfd#
User xpollfds

Defer >poll-events ( delay -- )
:noname
    0 xptimeout 2!
    epiper @ fileno  POLLIN  xpollfds 0 fds[]!
    infile-id fileno POLLIN  xpollfds 1 fds[]!
    dpy IF  dpy XConnectionNumber POLLIN  xpollfds 2 fds[]!  THEN
; IS >poll-events

: xpoll ( -- flag )
    [IFDEF] ppoll
	xptimeout 0 ppoll 0>
    [ELSE]
	xptimeout 2@ #1000 * swap #1000000 / + poll 0>
    [THEN] ;

Defer ?looper-timeouts ' noop is ?looper-timeouts
Defer looper-hook ( -- ) ' noop is looper-hook

: #looper ( delay -- ) #1000000 *
    ?looper-timeouts >poll-events
    dpy IF  dpy XPending IF  get-events ?events
	    looper-hook  EXIT  THEN  THEN
    xpollfds $@ pollfd / xpoll
    IF
	xpollfds $@ drop revents w@ POLLIN and IF  ?events  THEN
	dpy IF
	    dpy XPending  xpollfds $@ drop
	    [ pollfd 2* ]L + revents w@ POLLIN and or IF  get-events  THEN
	THEN
    THEN  looper-hook ;

: >looper ( -- )  looper-to# #looper ;
: >exposed  ( -- )  exposed off  BEGIN  >looper exposed @  UNTIL ;
: >select   ( -- )  got-selection off  BEGIN  >looper got-selection @  UNTIL ;
: select@ ( $addr -- addr u )
    >select  $@ ;
: ?looper ( -- )  ;

: clipboard@ ( -- addr u )
    dpy XA_CLIPBOARD XGetSelectionOwner IF
	dpy win XA_CLIPBOARD XDeleteProperty drop
	dpy XA_CLIPBOARD XA_STRING XA_CLIPBOARD win
	CurrentTime XConvertSelection drop
	paste$  select@
    ELSE
	s" "
    THEN ;

: primary@ ( -- addr u )
    dpy XA_PRIMARY XGetSelectionOwner IF
	dpy win XA_PRIMARY XDeleteProperty drop
	dpy XA_PRIMARY XA_STRING XA_PRIMARY win
	CurrentTime XConvertSelection drop
	primary$  select@
    ELSE
	s" "
    THEN ;

: set-protocol ( -- )
    dpy "WM_DELETE_WINDOW" 0 XInternAtom wm_delete_window l!
    dpy win over "WM_PROTOCOLS" 0 XInternAtom
    4 #32 1 wm_delete_window 1 XChangeProperty drop ;

: set-sync-request ( -- )
    dpy "_NET_WM_SYNC_REQUEST" 0 XInternAtom  wm_sync_request l!
    dpy win over "WM_PROTOCOLS" 0 XInternAtom
    4 #32 1 wm_sync_request 1 XChangeProperty drop

    #0. wm_sync_value xsv!
    dpy wm_sync_value XSyncCreateCounter wm_sync_counter l!
    dpy win over "_NET_WM_SYNC_REQUEST_COUNTER" 0 XInternAtom
    6 #32 1 wm_sync_counter 1 XChangeProperty drop ;

: set-compose-hint ( n -- ) { w^ compose }
    dpy win _NET_WM_BYPASS_COMPOSITOR 6 ( XA_CARDINAL )
    #32 1 compose 1 XChangeProperty drop ;

: set-fullscreen-hint ( -- )
    _NET_WM_STATE_FULLSCREEN { w^ fullscreen }
    dpy win _NET_WM_STATE 4 ( XA_ATOM )
    #32 1 fullscreen 1 XChangeProperty drop ;

: reset-fullscreen-hint ( -- )
    dpy win _NET_WM_STATE XDeleteProperty drop ;

XWMHints buffer: WMhints

: set-hint ( -- )  1 WMhints XWMHints-input l!
    NormalState WMhints XWMHints-initial_state l!
    [ InputHint StateHint or ] Literal
    WMhints XWMHints-flags !
    dpy win WMhints XSetWMHints drop ;

XSetWindowAttributes buffer: xswa

: set-xswa ( events -- )
    xswa XSetWindowAttributes-event_mask !
    NorthWestGravity xswa XSetWindowAttributes-bit_gravity l!
    NorthWestGravity xswa XSetWindowAttributes-win_gravity l! ;

: map-win ( -- )
    dpy win XMapWindow drop
    dpy 0 XSync drop >exposed ;

: simple-win ( events string len w h -- )
    2>r rot set-xswa 
    dpy dup XDefaultRootWindow
    0 0 2r>
    0 dpy dup XDefaultScreen XDefaultDepth InputOutput
    dpy dup XDefaultScreen XDefaultVisual
    [ CWEventMask CWBitGravity CWWinGravity or or ]L xswa XCreateWindow  to win
    dpy win 2swap XStoreName drop
    get-atoms  set-hint  set-protocol
    win get-ic ;

: term-key? ( -- flag )
    stdin isfg IF  defers key?  ELSE  key-buffer $@len 0>  THEN ;
: x-key? ( -- flag ) 0 #looper  term-key? dup 0= IF screen-ops THEN ;
: x-key ( -- key )
    +show  key? IF  defers key-ior  EXIT  THEN
    BEGIN  >looper  key? UNTIL  defers key-ior ;
: x-deadline ( dtime -- )
    up@ [ up@ ]L = IF screen-ops THEN  defers deadline ;
' x-deadline IS deadline

0 warnings !@
: bye ( -- )
    ic ?dup-IF  XDestroyIC  THEN  0 to ic
    xim ?dup-IF  XCloseIM drop  THEN  0 to xim
    bye ;
warnings !
' x-key IS key-ior
' x-key? IS key?
