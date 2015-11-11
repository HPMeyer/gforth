# First create a standalone toolchain directory
#mkdir ~/proj/android-toolchain
#cd  ~/proj/android-toolchain
#~/proj/android-ndk-r10e/build/tools/make-standalone-toolchain.sh --platform=android-21 --ndk-dir=/home/bernd/proj/android-ndk-r10e --install-dir=$PWD --toolchain=aarch64-linux-android-4.9
#configure with
#./configure --host=aarch64-linux-android --with-cross=android --prefix= --datarootdir=/sdcard --libdir=/sdcard --libexecdir=/lib --enable-lib --with-ditc=gforth-ditc
#and finally create an apk in this directory
#./build.sh
echo "Config for arm64-android"
XLIBS="sigaltstack.o __set_errno.o sigemptyset.o sigaddset.o termios.o"
(cd engine
mkdir .libs
for i in $XLIBS
do
  ar x ~/proj/android-toolchain-arm64/sysroot/usr/lib/libc.a $i
  cp $i .libs
  echo "# ${i%o}lo - a libtool object file
# Generated by libtool (GNU libtool) 2.4.2
#
# Please DO NOT delete this file!
# It is necessary for linking the library.

# Name of the PIC object.
pic_object='.libs/$i'

# Name of the non-PIC object
non_pic_object='$i'" >${i%o}lo
done
)
skipcode=".skip 16"
kernel_fi=kernl64l.fi
ac_cv_sizeof_void_p=8
ac_cv_sizeof_char_p=8
ac_cv_sizeof_char=1
ac_cv_sizeof_short=2
ac_cv_sizeof_int=4
ac_cv_sizeof_long=8
ac_cv_sizeof_long_long=8
ac_cv_sizeof_intptr_t=8
ac_cv_sizeof_int128_t=16
ac_cv_c_bigendian=no
ac_cv_func_memcmp_working=yes
ac_cv_func_memmove=yes
ac_cv_func_getpagesize=no
ac_cv_func_wcwidth=no
ac_cv_file___arch_arm64_asm_fs=yes
ac_cv_file___arch_arm64_disasm_fs=yes
ac_cv_func_dlopen=yes
ac_export_dynamic=no
CC=aarch64-linux-android-gcc
HOSTCC="gcc -m64"
GNU_LIBTOOL="aarch64-linux-android-libtool"
build_libcc_named=build-libcc-named
extraccdir=/data/app/gnu.gforth-1/lib/arm64
asm_fs=arch/arm64/asm.fs
disasm_fs=arch/arm64/disasm.fs
EC_MODE="false"
EXTRAPREFIX="\$(shell which \$(GCC) | sed -e s,/bin/\$(GCC),/sysroot/usr,g)"
NO_EC=""
EC=""
engine2='engine2$(OPT).o'
engine_fast2='engine-fast2$(OPT).o'
no_dynamic=""
image_i=""
LIBS="-llog -lz -lc"
signals_o="io.o signals.o $XLIBS androidmain.o zexpand.o"

