.PHONY: all clean

all:
	dotnet publish ApiDump -c Release --force

clean:
	dotnet clean -c Release -v minimal
