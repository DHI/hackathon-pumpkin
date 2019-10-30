import { from, of, throwError } from 'rxjs';
import { tap, map, flatMap, catchError } from 'rxjs/operators';

const DEFAULT_OPTIONS = {
  headers: {
    'Content-Type': 'application/json',
  },
};

const fetchUrl = (endPoint, options = {}) => {
  const mergedOptions = {
    ...DEFAULT_OPTIONS,
    ...options,
    headers: {
      ...options.headers,
    },
  };

  return from(fetch(endPoint, mergedOptions)).pipe(
    tap(response => console.log(`Response status: ${response.status}`)),
    map(response => {
      if (response.status >= 400) {
        throw new Error(`Error: ${response.status}, reason: ${response.statusText}`);
      } else {
        return response;
      }
    }),
    flatMap(response => (response.status !== 204 ? response.json() : of(response))),
    catchError(error => throwError(error)),
  );
};

const fetchMuseTrack = (host, id, includelocation) => {
  fetchUrl(`${host}/api/MuseTracks/${id}?includelocation=${includelocation}`, {
    method: 'GET',
  }).pipe(tap(res => console.log('fetched Muse Track', res)));
};

const fetchMuseTracks = (host, parameters) => {
  fetchUrl(`${host}/api/MuseTracks?from=${parameters.from}&to=${parameters.to}`, {
    method: 'GET',
  }).pipe(tap(res => console.log('fetched Muse Tracks', res)));
};

const fetchLastMuseTrack = (host, datetime) => {
  fetchUrl(`${host}/api/MuseTracks/LastBefore?datetime=${datetime}`, {
    method: 'GET',
  }).pipe(tap(res => console.log('fetched Last Muse Track', res)));
};

const fetchFirstMuseTrack = (host, datetime) => {
  fetchUrl(`${host}/api/MuseTracks/FirstAfter?datetime=${datetime}`, {
    method: 'GET',
  }).pipe(tap(res => console.log('fetched First Muse Track', res)));
};

const fetchRecentMuseTracks = (host, minutes) => {
  fetchUrl(`${host}/api/MuseTracks/RecentTracks?minutes=${minutes}`, {
    method: 'GET',
  }).pipe(tap(res => console.log('fetched Recent Muse Tracks', res)));
};

const createMuseTrack = (host, museTrack) => {
  fetchUrl(`${host}/api/MuseTracks`, {
    method: 'POST',
    body: { museTrack },
  });
};

const updateMuseTrack = (host, id, museTrack) => {
  fetchUrl(`${host}/api/MuseTracks/${id}`, {
    method: 'PUT',
    body: { museTrack },
  });
};

const deleteMuseTrack = (host, id, museTrack) => {
  fetchUrl(`${host}/api/MuseTracks/${id}`, {
    method: 'DELETE',
    body: { museTrack },
  });
};

export {
  fetchUrl,
  fetchMuseTrack,
  fetchMuseTracks,
  fetchLastMuseTrack,
  fetchFirstMuseTrack,
  fetchRecentMuseTracks,
  createMuseTrack,
  updateMuseTrack,
  deleteMuseTrack,
};
