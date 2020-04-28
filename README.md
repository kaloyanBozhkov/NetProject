![alt text](https://raw.githubusercontent.com/kaloyanBozhkov/NetProject/master/LogicMap.jpg)

<h1>Data manipulation and flow of ethernet packets over the physical layer of the ISO/OSI model</h1>
<p>This was my contribution to a group project during my second year in university. My role involved using C# to create a system that could send and receive CRUD instruction commands from one computer to another over the physical layer. This involved destructuring said comamnds into data packets (which could have payloads of various length), and parsing them back into the correct format upon being received in order for interpretation to happen as expected.
<br/><br/>
The main idea is that a central system (PrivateINet) can send commands to a device (ManufacturerDevice) on the ethernet physical layer. The central system is controlled entirely through master commands sent as UDP packets through localhost (SendUdpPacketsToPort).
  <br/><br/>The fun part of this project was playing around with UDP packets going through a port on localhsot as well as sending and reading Ethernet packets travelling between two specific network interfaces on the physical layer, controlled with the Pcap.Net library.  
  
<h3>Written in C#</h3>
<h4>Sending a command from PC-1</h4>

![alt text](https://raw.githubusercontent.com/kaloyanBozhkov/NetProject/master/SendCommandExample.jpg)

<h4>Receiving a command on PC-2</h4>

![alt text](https://raw.githubusercontent.com/kaloyanBozhkov/NetProject/master/ExampleOfManufacturerDeviceReceivingPackets.png)

</p>
