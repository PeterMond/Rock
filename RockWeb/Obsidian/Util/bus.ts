﻿/**
 * The bus allows page components to send and receive arbitrary data from other page components.
 */
// eslint-disable-next-line
// @ts-ignore
const bus = mitt();
const log = [];

/**
* Write a log entry that a payload was sent or received.
*/
const writeLog = (msg: string) => {
    log.push({
        date: new Date(),
        message: msg
    });
};

/**
* Send the payload to subscribers listening for the event name
*/
function publish<T>(eventName: string, payload: T) {
    writeLog(`Published ${eventName}`);
    bus.emit(eventName, payload);
};

/**
* Whenever an event is received of eventName, the callback is executed with the message
* payload as a parameter.
*/
function subscribe<T>(eventName: string, callback: (payload: T) => void) {
    writeLog(`Subscribed to ${eventName}`);
    bus.on(eventName, callback);
};

export default {
    publish,
    subscribe,
    log
};
