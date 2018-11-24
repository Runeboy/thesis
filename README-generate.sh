# requires imagemagick
set -x


filepath='tex/thesis.pdf'
outdir='README-img'
readmefilename='README.md'


# deduced vars
filename=`basename $filepath`

# convert pdf to images
convert -verbose -density 150 -quality 100 -alpha remove $filepath "${outdir}/${filename%.*}.jpg"


# write readme
echo 'A thesis project based on using a Kinect motion-tracker to investigate off-screen interactions beyond the display boundaries of portable devices.' \
	"See the [PDF version]($filepath) or, for the web-version, wait for this page to load." \
	> $readmefilename

pagecount=`ls $outdir | wc -l`
for i in `seq 1 $pagecount`
do 
	pageindex=`expr $i - 1`
	echo "<img src=\"$outdir/$filename-$pageindex\" alt=\"$filename-$pageindex\" width=\"100%\"/>" >> $readmefilename
done

