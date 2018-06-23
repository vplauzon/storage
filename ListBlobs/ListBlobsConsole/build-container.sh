#	Build docker container
sudo docker build -t vplauzon/list-blobs .

#	Publish image
sudo docker push vplauzon/list-blobs

#	Test image
#sudo docker run -it vplauzon/list-blobs