# ======================================================================================== #
# ======================================================================================== #
# ======================================================================================== #
# =====FUNCTION READS HAND IMAGES FROM FOLDER AND OUTPUTS HAND IMAGES WITH KEY POINTS===== #
# ======================================================================================== #
# ======================================================================================== #
# ======================================================================================== #


import cv2
import numpy
import math
import glob


# CNN information
protoFile = "hand/pose_deploy.prototxt"
weightsFile = "hand/pose_iter_102000.caffemodel"
nPoints = 22  # Number of keypoints generated by the skeleton CNN
POSE_PAIRS = [ [0,1],[1,2],[2,3],[3,4],[0,5],[5,6],[6,7],[7,8],[0,9],[9,10],[10,11],[11,12],[0,13],[13,14],[14,15],[15,16],[0,17],[17,18],[18,19],[19,20] ]
net = cv2.dnn.readNetFromCaffe(protoFile, weightsFile)
threshold = 0.1


# NOT NEEDED FOR PROCESSING
# Function to calculate distance in 2D
def dist2d(val_1, val_2):
    return math.sqrt((val_1[0] - val_2[0])**2 + (val_1[1] - val_2[1])**2)


# NOT NEEDED FOR PROCESSING
# Function to classify number of fingers held from skeleton information
def classify(keypoints):
    base = keypoints[0]
    f1 = keypoints[4]
    f2 = keypoints[8]
    f3 = keypoints[12]
    f4 = keypoints[16]
    f5 = keypoints[20]
    tips = [base, f1, f2, f3, f4, f5]

    # Can't recognize gesture
    if any(x is None for x in tips):
        return 0

    f1_b = dist2d(f1, base)
    f2_b = dist2d(f2, base)
    f3_b = dist2d(f3, base)
    f4_b = dist2d(f4, base)
    f5_b = dist2d(f5, base)

    fingers = [f1_b, f2_b, f3_b, f4_b, f5_b]
    closest = min(fingers)
    count = 0

    for x in fingers:
        if x > 2*closest:
            count = count + 1

    return count + 1


# Function applies neural network to hand image and overlays skeleton keypoints
def skeletize(image):
    picture = image  # preprocessed(bounding box/black background) picture
    pictureCopy = numpy.copy(picture)
    pictureWidth = picture.shape[1]
    pictureHeight = picture.shape[0]  # Save height&width to use when overlaying keypoints later
    resized = cv2.resize(picture, (224, 224))  # CNN works best with 224x224 inputs

    inpBlob = cv2.dnn.blobFromImage(resized, 1.0 / 255, (224, 224), (0, 0, 0), swapRB=False, crop=False)
    net.setInput(inpBlob)

    # This is where all the info is stored; the for loop below stores the 2d points in an array(?) called "points"
    output = net.forward()

    # Empty list to store the detected keypoints
    points = []

    for i in range(nPoints):
        # Create a sort of probability mass function corresponding to where it thinks point "n" is located on the image
        probMap = output[0, i, :, :]
        probMap = cv2.resize(probMap, (pictureWidth, pictureHeight))  # Resize from 224x224 to fit original image

        # Find global maxima of the probMap.
        minVal, prob, minLoc, point = cv2.minMaxLoc(probMap)

        # Use a preset minimum threshold; if max probability for point "n"is less that threshold then it is ignored
        if prob > threshold:
            cv2.circle(pictureCopy, (int(point[0]), int(point[1])), 8, (0, 255, 255), thickness=-1, lineType=cv2.FILLED)
            cv2.putText(pictureCopy, "{}".format(i), (int(point[0]), int(point[1])), cv2.FONT_HERSHEY_SIMPLEX, 1,
                        (0, 0, 255), 2, lineType=cv2.LINE_AA)

            # Add the point to the list if the probability is greater than the threshold
            points.append((int(point[0]), int(point[1])))
        else:
            points.append(None)

    # Draw Skeleton
    for pair in POSE_PAIRS:
        partA = pair[0]
        partB = pair[1]

        if points[partA] and points[partB]:
            cv2.line(picture, points[partA], points[partB], (0, 255, 255), 2)
            cv2.circle(picture, points[partA], 8, (0, 0, 255), thickness=-1, lineType=cv2.FILLED)
            cv2.circle(picture, points[partB], 8, (0, 0, 255), thickness=-1, lineType=cv2.FILLED)
    return (picture, points)


# Define which files you want to process (by first character in filename)
alphabet = ['m']

for letter in alphabet:
    # All the images I used were stored in the project workspace
    data_location = 'asl_dataset/' + letter + '/'
    save_location = 'Training_extra/'

    # Create array of images from dataset
    images = [cv2.imread(file) for file in glob.glob(data_location + '*.jpeg')]

    counter = 0
    for data in images:
        skeleton, points = skeletize(data)
        name = 'Training_extra/' + letter + str(counter) + '.jpg'
        retval = cv2.imwrite(name, skeleton)
        print(retval)
        print("Processing Image " + letter, counter, ".\n")
        counter = counter + 1



