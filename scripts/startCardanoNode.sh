#!/bin/bash
# Home directory for Cardano node
DIRECTORY=/home/danu/cardano-my-node
# Set a variable to indicate the port where the Cardano Node listens
PORT=6001
# 0.0.0.0 listens on all local IP addresses for the computer
HOSTADDR=0.0.0.0
# Set a variable to indicate the file path to your topology file
TOPOLOGY=${DIRECTORY}/mainnet-topology-relay.json
# Set a variable to indicate the folder where Cardano Node stores blockchain data
DB_PATH=${DIRECTORY}/db
# Set a variable to indicate the path to the Cardano Node socket for Inter-process communication (IPC)
SOCKET_PATH=${DIRECTORY}/db/socket
# Set a variable to indicate the file path to your main Cardano Node configuration file
CONFIG=${DIRECTORY}/config.json
# Run Cardano Node using the options that you set using variables
#
/usr/local/bin/cardano-node run --topology ${TOPOLOGY} --database-path ${DB_PATH} --socket-path ${SOCKET_PATH} --host-addr ${HOSTADDR} --port ${PORT} --config ${CONFIG}