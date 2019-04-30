\ Mini-OOF extension: current object in user variable  08jan13py

Variable class-o

: user-o ( "name" -- )
    0 uallot class-o !  User ;
: uval-o ( "name" -- )
    0 uallot class-o !  UValue ;

: umethod ( m v -- m' v )
    postpone nocov[
    over >r : postpone u#exec class-o @ , r> cell/ , postpone ;
    swap cell+ swap
    ['] umethod, set-optimizer ['] umethod! set-to ['] umethod@ set-defer@
    postpone ]nocov ;

: uvar ( m v size -- m v' )
    postpone nocov[
    over >r : postpone u#+ class-o @ , r> , postpone ; +
    ['] uvar, set-optimizer
    postpone ]nocov ;

: uclass ( c "name" -- c m v )
    ' execute next-task - class-o ! dup cell- cell- 2@ ;
