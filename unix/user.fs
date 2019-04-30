\ wrapper to load Swig-generated libraries

\ Copyright (C) 2015,2016,2017 Free Software Foundation, Inc.

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

cs-vocabulary user32
get-current also user32 definitions

c-library user
    \c #include <w32api/wtypes.h>
    \c #include <w32api/winuser.h>
    \c #include <w32api/imm.h>
    s" user32" add-lib
    s" imm32" add-lib
    include unix/winuser.fs
end-c-library

previous set-current
