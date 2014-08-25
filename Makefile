TARGETS = Stream.dll tests/TestClient.exe

all: ${TARGETS}

%.dll: %.cs
	mcs -t:library $<

%.exe: %.cs Stream.dll
	mcs -lib:. -r:Stream.dll $<

test: all
	MONO_PATH=. mono tests/TestClient.exe

clean:
	rm -f ${TARGETS}
	
.PHONY: all
