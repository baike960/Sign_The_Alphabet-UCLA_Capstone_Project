# ======================================================================================== #
# ======================================================================================== #
# ======================================================================================== #
# ==========MAIN PROCESS: READS IMAGES FROM CAMERA FEED TO CLASSIFY ASL LETTERS=========== #
# ======================================================================================== #
# ======================================================================================== #
# ======================================================================================== #


import cv2
import numpy
import math
import paho.mqtt.client as mqtt
import time


# ====FLAGS==== #
MODEL_FLAG = 1   # Sets whether or not alphabet classifier is to be used
COMM_FLAG = 0    # Sets whether or not MQTT communication is to be used
WEBCAM_FLAG = 1  # Sets whether or not a webcam is used


# Load alphabet classifying model
if MODEL_FLAG == 1:
    from keras.models import load_model
    alphabet = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u',
                'v', 'w', 'x', 'y', 'z']
    models = {'full': 'bad/test_model.h5', 'a-f': 'bad/a-f_model.h5', 'g-k': 'bad/g-k_model.h5',
              'l-p': 'bad/l-p_model.h5', 'q-u': 'bad/q-u_model.h5', 'v-z': 'bad/v-z_model.h5', 'custom': 'bad/custom.h5'}
    model_alphabets = {'full': numpy.arange(0, 26), 'a-f': numpy.arange(0, 6), 'g-k': numpy.arange(6, 11),
                       'l-p': numpy.arange(11, 16),'q-u:': numpy.arange(16, 21), 'v-z': numpy.arange(21, 26),
                       'custom': numpy.array([0, 1, 2, 4, 5, 6, 8, 10, 11, 12, 14, 15, 17, 25])}

    model_name_last = 'full'
    model_name = model_name_last
    model = load_model(models[model_name_last])
    model_range = model_alphabets[model_name_last]


# Define broker IP and port number for MQTT communication
broker = "131.179.4.137"
# broker = "test.mosquitto.org"
port = 1883


# Skeleton CNN information

protoFile = "hand/pose_deploy.prototxt"
weightsFile = "hand/pose_iter_102000.caffemodel"
nPoints = 22
POSE_PAIRS = [ [0,1],[1,2],[2,3],[3,4],[0,5],[5,6],[6,7],[7,8],[0,9],[9,10],[10,11],[11,12],[0,13],[13,14],[14,15],[15,16],[0,17],[17,18],[18,19],[19,20] ]
net = cv2.dnn.readNetFromCaffe(protoFile, weightsFile)
threshold = 0.1


# Initialize opencv windows for image output
cv2.namedWindow('Hand_Background', cv2.WINDOW_NORMAL)
cv2.setWindowProperty('Hand_Background', cv2.WND_PROP_FULLSCREEN, cv2.WINDOW_FULLSCREEN)
cv2.resizeWindow('Hand_Background', 650, 650)
cv2.moveWindow('Hand_Background', 0, 0)
cv2.namedWindow('Skeleton', cv2.WINDOW_NORMAL)
cv2.setWindowProperty('Skeleton', cv2.WND_PROP_FULLSCREEN, cv2.WINDOW_FULLSCREEN)
cv2.resizeWindow('Skeleton', 650, 650)
cv2.moveWindow('Skeleton', 650, 0)
cv2.namedWindow('Hand', cv2.WINDOW_NORMAL)
cv2.setWindowProperty('Hand', cv2.WND_PROP_FULLSCREEN, cv2.WINDOW_FULLSCREEN)
cv2.resizeWindow('Hand', 100, 100)
cv2.moveWindow('Hand', 550, 550)


# Function applies neural network to hand image to apply skeleton keypoints
def skeletize(image):
    picture = image
    pictureCopy = numpy.copy(picture)
    pictureWidth = picture.shape[1]
    pictureHeight = picture.shape[0]
    resized = cv2.resize(picture, (224, 224))
    inpBlob = cv2.dnn.blobFromImage(resized, 1.0 / 255, (224, 224), (0, 0, 0), swapRB=False, crop=False)
    net.setInput(inpBlob)

    output = net.forward()

    # Empty list to store the detected keypoints
    points = []

    for i in range(nPoints):
        # confidence map of corresponding body's part.
        probMap = output[0, i, :, :]
        probMap = cv2.resize(probMap, (pictureWidth, pictureHeight))

        # Find global maxima of the probMap.
        minVal, prob, minLoc, point = cv2.minMaxLoc(probMap)
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

    pointssq = []
    for x in points:
        if x is not None:
            pointssq.append((int((float(x[0]) / pictureWidth) * 400.0), int((float(x[1]) / pictureHeight) * 400.0)))
        else:
            pointssq.append(None)

    pointssq = [(000, 000) if v is None else v for v in points]

    # Uses pointssq (points normalized to 400x400) to make array formatted for classifier input
    x = [t[0] for t in pointssq]
    y = [t[1] for t in pointssq]
    data = numpy.column_stack((x[0:20], y[0:20]))
    data = numpy.reshape(data, 2 * len(data))
    data = numpy.asarray(data)
    data = data.astype(numpy.float64)

    return picture, points, data


# Function allows for user to recalibrate color values for skin
def calibrate_color(video):
    global total_rectangle
    cv2.namedWindow('Calibrate')

    # Get pointer to video frames from primary device
    camera = video

    is_hand_hist_created = False

    total_rectangle = 9
    hand_rect_one_x = None
    hand_rect_one_y = None

    hand_rect_two_x = None
    hand_rect_two_y = None

    # Draws rectangles on screen so user knows where to place their hand for calibration
    def draw_rect(picture):
        rows, cols, _ = picture.shape
        global total_rectangle, hand_rect_one_x, hand_rect_one_y, hand_rect_two_x, hand_rect_two_y
        hand_rect_one_x = numpy.array(
            [6 * rows / 20, 6 * rows / 20, 6 * rows / 20, 9 * rows / 20, 9 * rows / 20, 9 * rows / 20, 12 * rows / 20,
             12 * rows / 20, 12 * rows / 20], dtype=numpy.uint32)
        hand_rect_one_y = numpy.array(
            [9 * cols / 20, 10 * cols / 20, 11 * cols / 20, 9 * cols / 20, 10 * cols / 20, 11 * cols / 20,
             9 * cols / 20, 10 * cols / 20, 11 * cols / 20], dtype=numpy.uint32)

        hand_rect_two_x = hand_rect_one_x + 10
        hand_rect_two_y = hand_rect_one_y + 10

        for i in range(total_rectangle):
            cv2.rectangle(picture, (hand_rect_one_y[i], hand_rect_one_x[i]), (hand_rect_two_y[i], hand_rect_two_x[i]),
                          (0, 255, 0), 1)

        return picture

    # Takes hue values of hand
    def hand_histogram(picture):
        global hand_rect_one_x, hand_rect_one_y, min_YCrCb, max_YCrCb

        picture = cv2.cvtColor(picture, cv2.COLOR_BGR2YCR_CB)
        roi = numpy.zeros([90, 10, 3], dtype=picture.dtype)

        # Samples the colors in the rectangles
        for i in range(total_rectangle):
            roi[i * 10: i * 10 + 10, 0: 10, 0:3] = picture[hand_rect_one_x[i]:hand_rect_one_x[i] + 10,
                                                   hand_rect_one_y[i]:hand_rect_one_y[i] + 10, 0:3]

        # Takes YCrCb mean & std devs of the colors in the rectangles
        meanY, stdY = cv2.meanStdDev(roi[:, :, 0])
        meanCr, stdCr = cv2.meanStdDev(roi[:, :, 1])
        meanCb, stdCb = cv2.meanStdDev(roi[:, :, 2])

        # Determine min and max values of H using its mean
        if meanY[0, 0] > 80:
            Y_min = meanY[0, 0] - 80
        else:
            Y_min = 0

        if meanY[0, 0] < 175:
            Y_max = meanY[0, 0] + 80
        else:
            Y_max = 255

        if meanCr[0, 0] > 15:
            Cr_min = meanCr[0, 0] - 15
        else:
            Cr_min = 0

        if meanCr[0, 0] < 240:
            Cr_max = meanCr[0, 0] + 15
        else:
            Cr_max = 255

        if meanY[0, 0] > 10:
            Cb_min = meanCb[0, 0] - 10
        else:
            Cb_min = 0

        if meanY[0, 0] < 245:
            Cb_max = meanCb[0, 0] + 10
        else:
            Cb_max = 255
        # Create min and max HSV arrays to be returned
        min_YCrCb = numpy.array([0, Cr_min, Cb_min], numpy.uint8)
        max_YCrCb = numpy.array([255, Cr_max, Cb_max], numpy.uint8)

        return min_YCrCb, max_YCrCb

    while not is_hand_hist_created:
        global hand_hist, min_YCrCb, max_YCrCb

        pressed_key = cv2.waitKey(1)
        _, picture = camera.read()

        if pressed_key & 0xFF == ord('z'):
            is_hand_hist_created = True
            min_YCrCb, max_YCrCb = hand_histogram(picture)
        else:
            picture = draw_rect(picture)

        cv2.imshow("Calibrate", picture)

    cv2.destroyWindow('Calibrate')
    return min_YCrCb, max_YCrCb


# Function finds skin contours and applies bounding box and black background
def box(frame, min_YCrCb, max_YCrCb):
    # Convert image to YCrCb
    imageYCrCb = cv2.cvtColor(frame, cv2.COLOR_BGR2YCR_CB)
    imageYCrCb = cv2.GaussianBlur(imageYCrCb, (5, 5), 0)

    # Find region with skin tone in YCrCb image
    skinRegion = cv2.inRange(imageYCrCb, min_YCrCb, max_YCrCb)

    # Do contour detection on skin region
    contours, hierarchy = cv2.findContours(skinRegion, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)

    # Sort contours by area, largest area first
    contours = sorted(contours, key=cv2.contourArea, reverse=True)

    # Set largest contour to cnt
    if contours != []:
        cnt = contours[0]
    else:
        out = frame.copy()[0:10, 0:10]
        out_train = out
        return out, out_train

    # This is the image with black background around contour
    stencil = numpy.zeros(frame.shape).astype(frame.dtype)
    color = [255, 255, 255]
    cv2.fillPoly(stencil, [cnt], color)
    out_train = cv2.bitwise_and(frame, stencil)

    # Crop coordinates for hand
    x_crop, y_crop, w_crop, h_crop = cv2.boundingRect(cnt)

    # Find center of bounding box
    y_center = (h_crop // 2) + y_crop
    x_center = (w_crop // 2) + x_crop

    # Use longer rectangle length to ensure output is square
    length = max(w_crop, h_crop)

    # Create cropped image
    out = frame.copy()[max(0, y_center - (length // 2) - 10):y_center + (length // 2) + 10,
          max(0, x_center - (length // 2) - 10):x_center + (length // 2) + 10]

    # Create cropped image with black background
    out_train = out_train[max(0, y_center - (length // 2) - 10):y_center + (length // 2) + 10,
          max(0, x_center - (length // 2) - 10):x_center + (length // 2) + 10]

    return out, out_train


# MQTT client functions

def on_connect(client, userdata, flags, rc):
    if rc == 0:
        print("Connected to broker.")
        global Connected
        Connected = True
    else:
        print("Connection failed.")


def on_message(client, userdata, message):
    print("Message received: ", str(message.payload.decode("utf-8")))


def on_message_cmd(client, userdata, message):
    global command
    global first
    if first > 0:
        command = int(message.payload.decode("utf-8"))
        print("CMD message received: ", str(message.payload.decode("utf-8")))
    first = first + 1


def on_message_model(client, userdata, message):
    global model_name
    model_name = str(message.payload.decode("utf-8"))
    print("Model change request received.")


def on_publish(client, userdata, result):
    print("Data published.\n")
    pass


def on_subscribe(client, userdata, mid, granted_qos):
    print('Subscribed')


def on_log(client, userdata, level, buf):
    print("log: ", buf)


# YCrCb skin default ranges
Y_min = 0
Cr_min = 140  # Range from 140 to 150 makes huge change in sensitivity
Cb_min = 103
Y_max = 255
Cr_max = 182
Cb_max = 130

# Place default YCrCb skin tone ranges into arrays
min_YCrCb = numpy.array([Y_min, Cr_min, Cb_min], numpy.uint8)
max_YCrCb = numpy.array([Y_max, Cr_max, Cb_max], numpy.uint8)

# Get pointer to video frames from primary device
videoFrame = cv2.VideoCapture(WEBCAM_FLAG)

# Set up MQTT client
if COMM_FLAG == 1:
    Connected = False
    client = mqtt.Client()
    client.on_connect = on_connect
    client.on_message = on_message
    client.message_callback_add("command", on_message_cmd)
    client.message_callback_add("change_model", on_message_model)
    client.on_publish = on_publish
    client.on_subscribe = on_subscribe
    client.connect(broker, port)
    client.loop_start()
    while Connected is not True:
        time.sleep(.1)
    client.subscribe("command", qos=2)
    client.subscribe("change_model", qos=2)

# Initialize pressed key value to -1 (None pressed)
keyPressed = -1
command = 0  # Used in mqtt communication
first = 0    # Used to offset communication startup

# 'd' processes frame and 'z' begins calibration
while keyPressed < 0 or (keyPressed & 0xFF == ord('d')) or (keyPressed & 0xFF == ord('z')):
    keyPressed = -1

    # Read frame from video feed
    readSuccess, frame = videoFrame.read()

    # Crop and place black background on frame
    out, out_background = box(frame, min_YCrCb, max_YCrCb)

    # Display bounded camera feed with and without black background
    cv2.imshow('Hand', out)
    cv2.imshow('Hand_Background', out_background)

    # Check for user input to determine program behavior
    keyPressed = cv2.waitKey(20)  # wait 20 milliseconds in each iteration of while loop

    # Check to see if mqtt command has been received
    if command == 1:
        keyPressed = ord('d')

    # Change classification model if needed
    if MODEL_FLAG == 1:
        if model_name != model_name_last:
            print('Changing classification model to \"', model_name, '\" .')
            model = load_model(models[model_name])
            model_name_last = model_name
            model_range = model_alphabets[model_name]

    # If user presses "d" then program will take frame and send to neural network
    if keyPressed & 0xFF == ord('d'):
        skeleton, points, x_test = skeletize(out_background)  # x_test is 40x1 array w/ values scaled to 400x400 image
        if MODEL_FLAG == 1:
            val = model.predict_classes(numpy.array([x_test, ]))[0]  # Classifier output (number) using x_test
            letter = alphabet[model_range[val]]  # Retrieve letter corresponding to model output
            cv2.putText(skeleton, letter, (40, skeleton.shape[0] - 10), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)
            if COMM_FLAG == 1:
                client.publish("letter", "%c" % letter)
                command = 0
        cv2.imshow('Skeleton', skeleton)

    # If user presses "z" then program will allow for skin tone calibration
    if keyPressed & 0xFF == ord('z'):
        cv2.destroyWindow('Hand')
        cv2.destroyWindow('Hand_Background')
        cv2.destroyWindow('Skeleton')
        min_YCrCb, max_YCrCb = calibrate_color(videoFrame)
        cv2.namedWindow('Hand_Background', cv2.WINDOW_NORMAL)
        cv2.setWindowProperty('Hand_Background', cv2.WND_PROP_FULLSCREEN, cv2.WINDOW_FULLSCREEN)
        cv2.resizeWindow('Hand_Background', 650, 650)
        cv2.moveWindow('Hand_Background', 0, 0)
        cv2.namedWindow('Skeleton', cv2.WINDOW_NORMAL)
        cv2.setWindowProperty('Skeleton', cv2.WND_PROP_FULLSCREEN, cv2.WINDOW_FULLSCREEN)
        cv2.resizeWindow('Skeleton', 650, 650)
        cv2.moveWindow('Skeleton', 650, 0)
        cv2.namedWindow('Hand', cv2.WINDOW_NORMAL)
        cv2.setWindowProperty('Hand', cv2.WND_PROP_FULLSCREEN, cv2.WINDOW_FULLSCREEN)
        cv2.resizeWindow('Hand', 100, 100)
        cv2.moveWindow('Hand', 550, 550)


# Close window and camera after exiting the while loop
videoFrame.release()
cv2.destroyWindow('Hand')
cv2.destroyWindow('Hand_Background')
cv2.destroyWindow('Skeleton')
cv2.destroyAllWindows()
if COMM_FLAG == 1:
    client.unsubscribe("command")
    client.unsubscribe("change_model")
    client.disconnect()
    client.loop_stop()
