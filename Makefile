.PHONY: all clean

all:
	dotnet publish -c Release -r linux-x64 --force --no-self-contained -p:PublishSingleFile=true

clean:
	dotnet clean -c Release -v minimal
	dotnet clean -c Release -v minimal -r linux-x64
