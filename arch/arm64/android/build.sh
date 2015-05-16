#!/bin/bash

# takes as extra argument a directory where to look for .so-s

ENGINES="gforth-fast gforth-itc"

GFORTH_VERSION=$(gforth --version 2>&1 | cut -f2 -d' ')
APP_VERSION=$[$(cat ~/.app-version)+1]
echo $APP_VERSION >~/.app-version

sed -e "s/@VERSION@/$GFORTH_VERSION/g" -e "s/@APP@/$APP_VERSION/g" <AndroidManifest.xml.in >AndroidManifest.xml

if [ ! -f build.xml ]
then
    android update project -p . -s --target android-14
fi

SRC=../../..
LIBS=libs/arm64-v8a
LIBCCNAMED=lib/$(gforth --version 2>&1 | tr ' ' '/')/libcc-named/.libs
TOOLCHAIN=${TOOLCHAIN-~/proj/android-toolchain-arm64}

rm -rf $LIBS
mkdir -p $LIBS

if [ ! -f $TOOLCHAIN/sysroot/usr/lib/libsoil2.a ]
then
    cp $TOOLCHAIN/sysroot/usr/lib/libsoil.so $LIBS
fi
cp .libs/libtypeset.so $LIBS
strip $LIBS/lib{soil,typeset}.so

if [ "$1" != "--no-gforthgz" ]
then
    (cd $SRC
	if [ "$1" != "--no-config" ]; then ./configure --host=aarch64-linux-android --with-cross=android --with-ditc=gforth-ditc --prefix= --datarootdir=/sdcard --libdir=/sdcard --libexecdir=/lib --enable-lib || exit 1; fi
	make || exit 1
	make setup-debdist || exit 1) || exit 1
    if [ "$1" == "--no-config" ]; then CONFIG=no; shift; fi

    for i in . $*
    do
	cp $i/*.{fs,fi,png,jpg} $SRC/debian/sdcard/gforth/site-forth
    done
    (cd $SRC/debian/sdcard
	mkdir -p gforth/home
	gforth ../../archive.fs gforth/home/ $(find gforth -type f)) | gzip -9 >$LIBS/libgforthgz.so
else
    shift
fi

SHA256=$(sha256sum $LIBS/libgforthgz.so | cut -f1 -d' ')

for i in $ENGINES
do
    sed -e "s/sha256sum-sha256sum-sha256sum-sha256sum-sha256sum-sha256sum-sha2/$SHA256/" $SRC/engine/.libs/lib$i.so >$LIBS/lib$i.so
done

FULLLIBS=$PWD/$LIBS
ANDROID=${PWD%/*/*/*}
CFLAGS="-O3" 
LIBCC=$SRC
for i in $LIBCC $*
do
    (cd $i; test -d shlibs && \
	(cd shlibs
	    for j in *; do
		(cd $j
		    if [ "$CONFIG" == no ]
		    then
			make || exit 1
		    else
			./configure CFLAGS="$CFLAGS" --host=aarch64-linux-android && make clean && make && cp .libs/*.so $FULLLIBS || exit 1
		    fi
		)
	    done
	)
    )
    (cd $i; test -x ./libcc.android && ANDROID=$ANDROID ENGINE=gforth ./libcc.android)
    for j in $LIBCCNAMED .libs
    do
	for k in $(cd $i/$j; echo *.so)
	do
	    cp $i/$j/$k $LIBS
	done
    done
    shift
done
strip $LIBS/*.so
#ant debug
ant release
cp bin/Gforth-release.apk bin/Gforth.apk
#jarsigner -verbose -sigalg SHA1withRSA -digestalg SHA1 -keystore ~/.gnupg/bernd-release-key.keystore bin/Gforth$EXT.apk bernd