#!/usr/bin/env node
// Health check script for the Node.js application

const http = require('http');

const PORT = process.env.PORT || 3000;
const TIMEOUT = 10000; // 10 seconds

const options = {
  hostname: 'localhost',
  port: PORT,
  path: '/api/health',
  method: 'GET',
  timeout: TIMEOUT
};

const healthCheck = () => {
  const req = http.request(options, (res) => {
    if (res.statusCode === 200) {
      console.log('✅ Health check passed');
      process.exit(0);
    } else {
      console.log(`❌ Health check failed with status: ${res.statusCode}`);
      process.exit(1);
    }
  });

  req.on('timeout', () => {
    console.log('❌ Health check timed out');
    req.destroy();
    process.exit(1);
  });

  req.on('error', (err) => {
    console.log(`❌ Health check failed: ${err.message}`);
    process.exit(1);
  });

  req.setTimeout(TIMEOUT);
  req.end();
};

healthCheck();