#!/usr/bin/bash

rm -rf ./publish/Cache
rm -rf ./publish/ME2/bingo

./run_publish.sh

zip -r ./publish/EquipmentRandomizer.zip ./publish/EquipmentRandomizer
