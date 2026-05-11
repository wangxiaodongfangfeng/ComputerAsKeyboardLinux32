use serialport::{ClearBuffer, DataBits, Error, FlowControl, Parity, SerialPort, StopBits};
use std::collections::VecDeque;
use std::io::{self, Read, Write};
use std::time::Duration;

pub struct MockSerialPort {
    pub written_data: VecDeque<Vec<u8>>,
    read_data: VecDeque<u8>,
    timeout: Duration,
}

impl MockSerialPort {
    pub fn new() -> Self {
        MockSerialPort {
            written_data: VecDeque::new(),
            read_data: VecDeque::new(),
            timeout: Duration::from_millis(10),
        }
    }
}

impl Read for MockSerialPort {
    fn read(&mut self, buf: &mut [u8]) -> io::Result<usize> {
        if self.read_data.is_empty() {
            return Err(io::Error::new(io::ErrorKind::TimedOut, "Timeout"));
        }
        
        let len = std::cmp::min(buf.len(), self.read_data.len());
        for i in 0..len {
            buf[i] = self.read_data.pop_front().unwrap();
        }
        Ok(len)
    }
}

impl Write for MockSerialPort {
    fn write(&mut self, data: &[u8]) -> io::Result<usize> {
        self.written_data.push_back(data.to_vec());
        Ok(data.len())
    }
    
    fn flush(&mut self) -> io::Result<()> {
        Ok(())
    }
}

impl SerialPort for MockSerialPort {
    fn baud_rate(&self) -> Result<u32, Error> {
        Ok(9600)
    }

    fn set_baud_rate(&mut self, _baud_rate: u32) -> Result<(), Error> {
        Ok(())
    }

    fn data_bits(&self) -> Result<DataBits, Error> {
        Ok(DataBits::Eight)
    }

    fn set_data_bits(&mut self, _data_bits: DataBits) -> Result<(), Error> {
        Ok(())
    }

    fn flow_control(&self) -> Result<FlowControl, Error> {
        Ok(FlowControl::None)
    }

    fn set_flow_control(&mut self, _flow_control: FlowControl) -> Result<(), Error> {
        Ok(())
    }

    fn parity(&self) -> Result<Parity, Error> {
        Ok(Parity::None)
    }

    fn set_parity(&mut self, _parity: Parity) -> Result<(), Error> {
        Ok(())
    }

    fn stop_bits(&self) -> Result<StopBits, Error> {
        Ok(StopBits::One)
    }

    fn set_stop_bits(&mut self, _stop_bits: StopBits) -> Result<(), Error> {
        Ok(())
    }

    fn timeout(&self) -> Duration {
        self.timeout
    }

    fn set_timeout(&mut self, timeout: Duration) -> Result<(), Error> {
        self.timeout = timeout;
        Ok(())
    }

    fn clear(&self, _buffer: ClearBuffer) -> Result<(), Error> {
        Ok(())
    }

    fn name(&self) -> Option<String> {
        Some("mock_serial_port".to_string())
    }

    fn bytes_to_read(&self) -> Result<u32, Error> {
        Ok(self.read_data.len() as u32)
    }

    fn bytes_to_write(&self) -> Result<u32, Error> {
        Ok(0)
    }

    fn write_request_to_send(&mut self, _level: bool) -> Result<(), Error> {
        Ok(())
    }

    fn write_data_terminal_ready(&mut self, _level: bool) -> Result<(), Error> {
        Ok(())
    }

    fn read_clear_to_send(&mut self) -> Result<bool, Error> {
        Ok(true)
    }

    fn read_data_set_ready(&mut self) -> Result<bool, Error> {
        Ok(true)
    }

    fn read_ring_indicator(&mut self) -> Result<bool, Error> {
        Ok(false)
    }

    fn read_carrier_detect(&mut self) -> Result<bool, Error> {
        Ok(true)
    }

    fn try_clone(&self) -> Result<Box<dyn SerialPort>, Error> {
        Ok(Box::new(MockSerialPort {
            written_data: self.written_data.clone(),
            read_data: self.read_data.clone(),
            timeout: self.timeout,
        }))
    }

    fn set_break(&self) -> Result<(), Error> {
        Ok(())
    }

    fn clear_break(&self) -> Result<(), Error> {
        Ok(())
    }
}

unsafe impl Send for MockSerialPort {}
