#!/bin/bash
#for i in *.svg; do
#  rsvg-convert -w 500 -h 500 "$i" -o "${i%.svg}.png"
#done

find . -name "*.svg" -exec sh -c 'rsvg-convert -w 500 -h 500 "$0" -o "${0%.svg}.png"' {} \;
