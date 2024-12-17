#!/usr/bin/bash

rm -rf ./publish/ZeroScratchBingoRandomizer/Cache
rm -rf ./publish/ZeroScratchBingoRandomizer/ME2
# rm ./publish/ZeroScratchBingoRandomizer.zip

./run_publish.sh && \
cd ./publish && \
zip -r ./$1.zip ./ZeroScratchBingoRandomizer
