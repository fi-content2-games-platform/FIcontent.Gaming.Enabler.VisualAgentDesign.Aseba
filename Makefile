all: Stream.dll tests/TestClient.exe

%.dll: %.cs
	mcs -t:library $<

%.exe: %.cs Stream.dll
	mcs -lib:. -r:Stream.dll $<

test: all
	MONO_PATH=. mono tests/TestClient.exe
	
.PHONY: all
