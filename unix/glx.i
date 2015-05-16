// this file is in the public domain
%module glx
%insert("include")
%{
#include <GL/glx.h>
%}
%apply int { XID, Bool, GLsizei, Pixmap, Font, Window };
%apply long long { int64_t };
%apply float { GLfloat };

// exec: sed -e 's/\(c-function glXQuery.*Renderer\)/\\ \1/g' -e 's/\(c-function glX.*TexImageEXT\)/\\ \1/g'

%include <GL/glx.h>