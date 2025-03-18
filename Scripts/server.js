const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 8080 });

const rooms = new Map();

wss.on('connection', (ws, req) => {
    console.log('New connection');
    const url = new URL(req.url, 'http://localhost:8080');
    const roomCode = url.searchParams.get('room') || 'default';
    const isCreate = req.url.startsWith('/create');

    if (!rooms.has(roomCode)) {
        if (isCreate) {
            rooms.set(roomCode, []);
            console.log(`Room ${roomCode} created`);
        } else {
            ws.close(1000, 'Room does not exist');
            return;
        }
    }

    const room = rooms.get(roomCode);
    if (room.length >= 2) {
        ws.close(1000, 'Room is full');
        return;
    }

    room.push(ws);
    console.log(`Player joined room ${roomCode}. Players: ${room.length}`);

    ws.on('message', (message) => {
        room.forEach(client => {
            if (client.readyState === WebSocket.OPEN) {
                client.send(message); // Пересылаем всем, включая отправителя
            }
        });
    });

    ws.on('close', () => {
        const index = room.indexOf(ws);
        if (index > -1) {
            room.splice(index, 1);
            console.log(`Player left room ${roomCode}. Players: ${room.length}`);
            if (room.length === 0) {
                rooms.delete(roomCode);
            }
        }
    });

    if (room.length === 2) {
        room.forEach(client => {
            if (client.readyState === WebSocket.OPEN) {
                client.send(JSON.stringify({ type: 'start', roomCode }));
            }
        });
    }
});

console.log('WebSocket server running on ws://localhost:8080');