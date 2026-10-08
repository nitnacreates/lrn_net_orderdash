<?xml version="1.0" encoding="UTF-8"?>
<!-- Canonical order response -> partner ack/shipment XML (§16). -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" indent="yes" />

  <xsl:template match="/response">
    <OrderAck>
      <PurchaseOrderNumber><xsl:value-of select="orderNumber" /></PurchaseOrderNumber>
      <StatusCode><xsl:value-of select="status" /></StatusCode>
      <Reason><xsl:value-of select="reason" /></Reason>
      <DespatchDate><xsl:value-of select="dispatchedDate" /></DespatchDate>
      <Carrier><xsl:value-of select="carrier" /></Carrier>
      <Tracking><xsl:value-of select="trackingNumber" /></Tracking>
    </OrderAck>
  </xsl:template>
</xsl:stylesheet>
