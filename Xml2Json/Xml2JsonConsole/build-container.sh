#	Build docker container
sudo docker build -t vplauzon/xml2json .

#	Publish image
sudo docker push vplauzon/xml2json

#	Test image
#sudo docker run -it vplauzon/xml2json