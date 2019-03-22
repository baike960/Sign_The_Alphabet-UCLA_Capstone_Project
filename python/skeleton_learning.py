# ======================================================================================== #
# ======================================================================================== #
# ======================================================================================== #
# ====FUNCTION USES 2D KEYPOINT ARRAY INFORMATION TO TRAIN CUSTOMIZABLE NEURAL NETWORK==== #
# ======================================================================================== #
# ======================================================================================== #
# ======================================================================================== #


import numpy
import glob
from keras import optimizers
from keras.models import Sequential, load_model
from keras.layers.core import Dense, Activation, Flatten, Dropout
from keras.utils import np_utils
import matplotlib.pyplot as plt


# Some methods for indexing alphabet:
# numpy.where(YL[0:10]==1)[1] # Gives array of alphabet indices for each data label e.g. 0 corresponds to 'a'
# numpy.asarray(list(alphabet.keys())) # Gives array of alphabet characters
# model.predict_classes(data[0:1]) returns number corresponding to most likely alphabet value

NUM_CHARS = 7  # Number of characters in alphabet to be used for training

alphabet = {'a':0, 'b':1,'c':2,'d':3,'e':4,'f':5,'g':6,'h':7,'i':8,'j':9,'k':10,'l':11,'m':12,'n':13,
            'o':14,'p':15,'q':16,'r':17,'s':18,'t':19,'u':20,'v':21,'w':22,'x':23,'y':24,'z':25}

custom = {'a':0, 'b':1,'c':2,'e':3,'f':4,'g':5,'i':6,'k':7,'l':8,'m':9,
            'o':10,'p':11,'r':12,'z':13}


def baseline_model():
    # Create model
    model = Sequential()
    model.add(Dense(128, input_dim=40, activation='relu'))
    model.add(Dropout(0.2))
    model.add(Dense(256, activation='relu'))
    model.add(Dropout(0.3))
    model.add(Dense(64, activation='relu'))
    model.add(Dropout(0.5))
    model.add(Dense(32, activation='relu'))
    model.add(Dropout(0.5))
    model.add(Dense(NUM_CHARS, activation='softmax'))

    # Compile model
    Adadelta = optimizers.Adadelta(lr = 1)
    model.compile(loss='categorical_crossentropy', optimizer=Adadelta, metrics=['accuracy'])
    return model


# Specify which letters to be read in the glob argument
data = [numpy.loadtxt(file) for file in glob.glob('keypoints/[abcdefg]*.dat')]
YL = numpy.zeros(len(data))

# Reshape data from list of 2D arrays to one double-length array (x1 y1 x2 y2 ...)
for p in range(0, len(data)):
    data[p] = numpy.reshape(data[p], 2*len(data[p]))

counter = 0
# Specify which letters to be read in the glob argument
for file in glob.glob('keypoints/[abcdefg]*.dat'):
    YL[counter] = alphabet[file[10]]       # 11th character in filename corresponds to alphabet letter
    counter = counter + 1

data = numpy.asarray(data)
YL = np_utils.to_categorical(YL)

# Shuffle data so that initial validation split is valid
index = numpy.arange(data.shape[0])
numpy.random.shuffle(index)
data = data[index]
YL = YL[index]

model = baseline_model()

history = model.fit(x = data, y = YL, validation_split=0.2, shuffle=True, epochs=1000, batch_size=96)

model.save('bad/safety.h5')

plt.plot(history.history['acc'])
plt.plot(history.history['val_acc'])
plt.title('model accuracy')
plt.ylabel('accuracy')
plt.xlabel('epoch')
plt.legend(['train', 'test'], loc='upper left')
plt.show()
# summarize history for loss
plt.plot(history.history['loss'])
plt.plot(history.history['val_loss'])
plt.title('model loss')
plt.ylabel('loss')
plt.xlabel('epoch')
plt.legend(['train', 'test'], loc='upper left')
plt.show()
